using AstrophotographyBuddy.Utility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AstrophotographyBuddy.Model {
    class AstrometryPlateSolver : BaseINPC, IPlateSolver {

        const string AUTHURL = "http://nova.astrometry.net/api/login";
        const string UPLOADURL = "http://nova.astrometry.net/api/upload";
        const string SUBMISSIONURL = "http://nova.astrometry.net/api/submissions/{0}";
        const string JOBSTATUSURL = "http://nova.astrometry.net/api/jobs/{0}";
        const string JOBINFOURL = "http://nova.astrometry.net/api/jobs/{0}/info/";
        const string ANNOTATEDIMAGEURL = "http://nova.astrometry.net/annotated_display/{0}";



        public AstrometryPlateSolver() {

        }

        private async Task<JObject> authenticate(CancellationTokenSource canceltoken) {
            string apikey = Settings.AstrometryAPIKey;

            string json = "{\"apikey\":\"" + apikey + "\"}";
            json = Utility.Utility.encodeUrl(json);
            string body = "request-json=" + json;

            string response = await Utility.Utility.httpPostRequest(AUTHURL, body, canceltoken);

            JObject o = JObject.Parse(response);

            return o;
        }

        private async Task<JObject> submitImage(MemoryStream ms, string session, CancellationTokenSource canceltoken) {
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("request-json", "{\"publicly_visible\": \"n\", \"allow_modifications\": \"d\", \"session\": \"" + session + "\", \"allow_commercial_use\": \"d\"}");
            string response = await Utility.Utility.httpUploadFile(UPLOADURL, ms, "file", "image/jpeg", nvc, canceltoken);
            JObject o = JObject.Parse(response);

            return o;
        }

        private async Task<JObject> getSubmissionStatus(string subid, CancellationTokenSource canceltoken) {
            string response = await Utility.Utility.httpGetRequest(canceltoken,SUBMISSIONURL, subid);
            JObject o = JObject.Parse(response);

            return o;
        }

        private async Task<JObject> getJobStatus(string jobid, CancellationTokenSource canceltoken) {
            string response = await Utility.Utility.httpGetRequest(canceltoken,JOBSTATUSURL, jobid);
            JObject o = JObject.Parse(response);

            return o;
        }

        private async Task<JObject> getJobInfo(string jobid, CancellationTokenSource canceltoken) {
            string response = await Utility.Utility.httpGetRequest(canceltoken, JOBINFOURL, jobid);
            JObject o = JObject.Parse(response);

            return o;
        }

        private async Task<BitmapImage> getJobImage(string jobid, CancellationTokenSource canceltoken) {
            return await Utility.Utility.httpGetImage(canceltoken, ANNOTATEDIMAGEURL, jobid);
        }

        public async Task<PlateSolveResult> blindSolve(MemoryStream image, IProgress<string> progress, CancellationTokenSource canceltoken) {
            PlateSolveResult result = new PlateSolveResult();

            try {

                progress.Report("Authenticating...");
                JObject authentication = await authenticate(canceltoken);
                var status = authentication.GetValue("status");
                string session = string.Empty;
                if (status != null && status.ToString() == "success") {
                    session = authentication.GetValue("session").ToString();

                    progress.Report("Uploading Image...");
                    JObject imagesubmission = await submitImage(image, session, canceltoken);

                    string subid = string.Empty;
                    string jobid = string.Empty;
                    if (imagesubmission.GetValue("status") != null && imagesubmission.GetValue("status").ToString() == "success") {
                        subid = imagesubmission.GetValue("subid").ToString();

                        
                        progress.Report("Waiting for plate solve to start ...");
                        while (true) {
                            canceltoken.Token.ThrowIfCancellationRequested();

                            JObject submissionstatus = await getSubmissionStatus(subid, canceltoken);

                            JArray jobids;
                            jobids = (JArray)submissionstatus.GetValue("jobs");
                            if (jobids.Count > 0) {
                                jobid = jobids.First.ToString();
                                if (jobid != "") {
                                    break;
                                }

                            }
                            await Task.Delay(1000);

                        };

                        if (!string.IsNullOrWhiteSpace(jobid)) {

                            string jobstatus = string.Empty;
                            progress.Report("Solving ...");
                            while (true) {
                                canceltoken.Token.ThrowIfCancellationRequested();
                                JObject ojobstatus = await getJobStatus(jobid, canceltoken);
                                jobstatus = ojobstatus.GetValue("status").ToString();

                                if ((jobstatus == "failure") || (jobstatus == "success")) {
                                    break;
                                }
                                await Task.Delay(1000);

                            };

                            if (jobstatus == "success") {
                                progress.Report("Getting plate solve result ...");
                                JObject job = await getJobInfo(jobid, canceltoken);
                                JobResult jobinfo = job.ToObject<JobResult>();

                                result.Dec = jobinfo.calibration.dec;
                                result.Orientation = jobinfo.calibration.orientation;
                                result.Pixscale = jobinfo.calibration.pixscale;
                                result.Ra = jobinfo.calibration.ra;
                                result.Radius = jobinfo.calibration.radius;

                                result.SolvedImage = await getJobImage(jobid, canceltoken);
                                progress.Report("Done");
                            }
                            else {
                                progress.Report("Plate solve failed");
                            }
                        }
                        else {
                            progress.Report("Failed to get job result");
                        }
                    }
                    else {
                        progress.Report("Failed to get submission");
                    }
                }
                else {
                    progress.Report("Authorization failed ...");
                }

            }

            catch (System.OperationCanceledException ex) {
                Logger.trace(ex.Message);
                progress.Report("Cancelled");
            }
            finally {
               
            }
            return result;
        }

    }

    public class JobResult {
        public string status;
        public string[] machine_tags;
        public Calibration calibration;
        public string[] tags;
        public string original_filename;
        public string[] objects_in_field;
    }
    public class Calibration {
        public double parity;
        public double orientation;
        public double pixscale;
        public double radius;
        public double ra;
        public double dec;
    }
}
