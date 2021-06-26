#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Core.Utility.Http;
using NINA.Plugin.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Plugin {

    public class PluginInstaller {

        /// <summary>
        /// Plugins will be uninstalled by moving the folder inside {Constants.UserExtensionsFolder}//{manifest.Name} into the deletion area {Constatns.DeletionFolder}
        /// If that directory does not exist, it is assumed that the dll is directly put into the root folder. For that a search for the dll with the matching guid is searched
        /// </summary>
        /// <param name="manifest"></param>
        public void Uninstall(IPluginManifest manifest) {
            try {
                if (!Directory.Exists(Constants.DeletionFolder)) {
                    Directory.CreateDirectory(Constants.DeletionFolder);
                }
                var folder = GetDestinationFolderFromManifest(manifest);
                if (Directory.Exists(folder)) {
                    MoveDirectoryContentToDeletion(folder);
                } else {
                    var file = FindFileInRootFolderByGuid(manifest);
                    if (!string.IsNullOrWhiteSpace(file)) {
                        var dir = Path.GetDirectoryName(file);
                        if (dir == Constants.UserExtensionsFolder) {
                            File.Move(file, Path.Combine(Constants.DeletionFolder, Path.GetRandomFileName()));
                        } else {
                            // This should only happen when the directory where the plugin resides does not match the conventions or was renamed
                            MoveDirectoryContentToDeletion(dir);
                        }
                    } else {
                        throw new FileNotFoundException();
                    }
                }
            } catch (Exception ex) {
                Logger.Error($"Failed to move plugin {manifest.Name} to deletion location", ex);
                throw ex;
            }
        }

        public async Task Install(IPluginManifest manifest, bool update, CancellationToken ct) {
            var tempFile = Path.Combine(Path.GetTempPath(), manifest.Name + ".dll");
            try {
                if (!Directory.Exists(Constants.StagingFolder)) {
                    Directory.CreateDirectory(Constants.StagingFolder);
                }
                if (!Directory.Exists(Constants.DeletionFolder)) {
                    Directory.CreateDirectory(Constants.DeletionFolder);
                }

                try {
                    using (WebClient client = new WebClient()) {
                        using (ct.Register(() => client.CancelAsync(), useSynchronizationContext: false)) {
                            using (Stream rawStream = client.OpenRead(manifest.Installer.URL)) {
                                string contentDisposition = client.ResponseHeaders["content-disposition"];
                                if (!string.IsNullOrEmpty(contentDisposition)) {
                                    string lookFor = "filename=";
                                    int index = contentDisposition.IndexOf(lookFor, StringComparison.CurrentCultureIgnoreCase);
                                    if (index >= 0)
                                        tempFile = Path.Combine(Path.GetTempPath(), contentDisposition.Substring(index + lookFor.Length).Replace("\"", ""));
                                }

                                /*client.DownloadProgressChanged += (s, e) => {
                                    progress?.Report(e.ProgressPercentage);
                                };*/
                                Logger.Info($"Downloading plugin from {manifest.Installer.URL}");
                                await client.DownloadFileTaskAsync(manifest.Installer.URL, tempFile);
                            }
                        }
                    }
                } catch (WebException ex) {
                    if (ex.Status == WebExceptionStatus.RequestCanceled) {
                        throw new OperationCanceledException();
                    } else {
                        throw ex;
                    }
                }

                var req = new HttpDownloadFileRequest(manifest.Installer.URL, tempFile);

                await req.Request(ct);

                if (!ValidateChecksum(tempFile, manifest)) {
                    throw new Exception($"Checksum of file {tempFile} does not match expected checksum {manifest.Installer.Checksum} of type {manifest.Installer.ChecksumType}. File may be corrupted");
                }

                if (update) {
                    try {
                        Uninstall(manifest);
                    } catch (Exception ex) {
                        Logger.Error($"Failed to uninstall plugin {manifest.Name}", ex);
                    }
                }

                switch (manifest.Installer.Type) {
                    /*case ManifestDefinition.InstallerType.EXE:
                        //Default = DLL
                        await InstallExe(manifest, tempFile);
                        break;

                    case ManifestDefinition.InstallerType.MSI:
                        await InstallMSI(manifest, tempFile);
                        break;*/

                    case ManifestDefinition.InstallerType.ARCHIVE:
                        await InstallArchive(manifest, tempFile);
                        break;

                    default:
                        //Default = DLL
                        await InstallDLL(manifest, tempFile);
                        break;
                }
            } finally {
                //Cleanup
                try {
                    if (File.Exists(tempFile)) {
                        File.Delete(tempFile);
                    }
                } catch (Exception ex) {
                    Logger.Error("Failed to delete plugin temp file", ex);
                }
            }

            return;
        }

        private void MoveDirectoryContentToDeletion(string dir) {
            foreach (var file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)) {
                try {
                    File.Move(file, Path.Combine(Constants.DeletionFolder, Path.GetRandomFileName()));
                } catch (Exception ex) {
                    Logger.Error($"Failed to move file to deletion location {file} due to {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Find the plugin dll by the guid in the root extension folder if it exists by looking up the GuidAttribute
        /// </summary>
        /// <param name="manifest"></param>
        /// <returns></returns>
        private string FindFileInRootFolderByGuid(IPluginManifest manifest) {
            foreach (var file in Directory.GetFiles(Constants.UserExtensionsFolder, "*.dll", SearchOption.AllDirectories)) {
                try {
                    var reflectionAssembly = Assembly.ReflectionOnlyLoadFrom(file);

                    var attr = CustomAttributeData.GetCustomAttributes(reflectionAssembly);
                    var id = attr.First(x => x.AttributeType == typeof(GuidAttribute)).ConstructorArguments.First().Value.ToString();
                    if (id == manifest.Identifier) {
                        return file;
                    }
                } catch (Exception) {
                }
            }
            return string.Empty;
        }

        private bool ValidateChecksum(string tempFile, IPluginManifest manifest) {
            using (var algorithm = GetAlgorithm(manifest)) {
                using (var stream = File.OpenRead(tempFile)) {
                    var fileChecksum = BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty);
                    return fileChecksum == manifest.Installer.Checksum;
                }
            }
        }

        private HashAlgorithm GetAlgorithm(IPluginManifest manifest) {
            switch (manifest.Installer.ChecksumType) {
                case ManifestDefinition.InstallerChecksum.MD5:
                    return MD5.Create();

                case ManifestDefinition.InstallerChecksum.SHA1:
                    return SHA1.Create();

                case ManifestDefinition.InstallerChecksum.SHA256:
                default:
                    return SHA256.Create();
            }
        }

        private async Task InstallDLL(IPluginManifest manifest, string sourceFile) {
            Logger.Info($"Installing plugin DLL {manifest.Name}");
            var name = Path.GetFileName(sourceFile);
            var destination = Path.Combine(GetDestinationFolderFromManifest(manifest), name);

            using (Stream source = File.Open(sourceFile, FileMode.Open)) {
                if (!Directory.Exists(Path.GetDirectoryName(destination))) {
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                }

                using (Stream destinationStream = File.Create(destination)) {
                    await source.CopyToAsync(destinationStream);
                }
            }
        }

        private string GetDestinationFolderFromManifest(IPluginManifest manifest) {
            return Path.Combine(Constants.UserExtensionsFolder, CoreUtil.ReplaceAllInvalidFilenameChars(manifest.Name));
        }

        private Task InstallArchive(IPluginManifest manifest, string tempFile) {
            Logger.Info($"Installing plugin archive {manifest.Name}");
            var destination = GetDestinationFolderFromManifest(manifest);

            using (ZipArchive archive = ZipFile.Open(tempFile, ZipArchiveMode.Read)) {
                archive.ExtractToDirectory(destination, true);
            }
            return Task.CompletedTask;
        }

        private Task InstallExe(IPluginManifest manifest, string tempFile) {
            throw new NotImplementedException();
        }

        private Task InstallMSI(IPluginManifest manifest, string tempFile) {
            throw new NotImplementedException();
        }
    }

    public static class ZipArchiveExtensions {

        public static void ExtractToDirectory(this ZipArchive archive, string destinationDirectoryName, bool overwrite) {
            if (!overwrite) {
                archive.ExtractToDirectory(destinationDirectoryName);
                return;
            }

            DirectoryInfo di = Directory.CreateDirectory(destinationDirectoryName);
            string destinationDirectoryFullPath = di.FullName;

            foreach (ZipArchiveEntry file in archive.Entries) {
                string completeFileName = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, file.FullName));

                if (!completeFileName.StartsWith(destinationDirectoryFullPath, StringComparison.OrdinalIgnoreCase)) {
                    throw new IOException("Trying to extract file outside of destination directory. See this link for more info: https://snyk.io/research/zip-slip-vulnerability");
                }

                if (file.Name == "") {// Assuming Empty for Directory
                    Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                    continue;
                }
                file.ExtractToFile(completeFileName, true);
            }
        }
    }
}