using AstrophotographyBuddy.Utility;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AstrophotographyBuddy.Model {
    class AstrometryPlateSolver : BaseINPC, IPlateSolver  {

        const string AUTHURL = "http://nova.astrometry.net/api/login";
        const string UPLOADURL = "http://nova.astrometry.net/api/upload";
        const string SUBMISSIONURL = "http://nova.astrometry.net/api/submissions/{0}";
        const string JOBSTATUSURL = "http://nova.astrometry.net/api/jobs/{0}";
        const string JOBINFOURL = "http://nova.astrometry.net/api/jobs/{0}/info/";
        const string ANNOTATEDIMAGEURL = "http://nova.astrometry.net/annotated_display/{0}";


        public AstrometryPlateSolver() {
        
        }

        private async Task<JObject> authenticate() {            
            string apikey = Settings.AstrometryAPIKey;

            string json = "{\"apikey\":\"" + apikey + "\"}";
            json = Utility.Utility.encodeUrl(json);
            string body = "request-json=" + json;

            string response = await Utility.Utility.httpPostRequest(AUTHURL, body);

            JObject o = JObject.Parse(response);

            return o;
        }

        private async Task<JObject> submitImage(BitmapFrame image, string session) {       
            /* Read image into memorystream */
            var ms = new MemoryStream();
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(image);
            encoder.QualityLevel = 90;
            encoder.Save(ms);
            ms.Seek(0, SeekOrigin.Begin);

            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("request-json", "{\"publicly_visible\": \"n\", \"allow_modifications\": \"d\", \"session\": \"" + session + "\", \"allow_commercial_use\": \"d\"}");
            string response = await Utility.Utility.httpUploadFile(UPLOADURL, ms, "file", "image/jpeg", nvc);
            JObject o = JObject.Parse(response);

            return o;
        }

        private async Task<JObject> getSubmissionStatus(string subid) {    
            string response = await Utility.Utility.httpGetRequest(SUBMISSIONURL, subid);
            JObject o = JObject.Parse(response);

            return o;
        }

        private async Task<JObject> getJobStatus(string jobid) {
            string response = await Utility.Utility.httpGetRequest(JOBSTATUSURL, jobid);
            JObject o = JObject.Parse(response);

            return o;
        }

        private async Task<JObject> getJobInfo(string jobid) {
            string response = await Utility.Utility.httpGetRequest(JOBINFOURL, jobid);
            JObject o = JObject.Parse(response);

            return o;
        }

        private async Task<BitmapImage> getJobImage(string jobid) {
            return await Utility.Utility.httpGetImage(ANNOTATEDIMAGEURL, jobid);
        }

        public async Task<PlateSolveResult> blindSolve(BitmapSource source) {
            PlateSolveResult result = new PlateSolveResult(); 
            BitmapFrame image = null;
            /* Resize Image */
            if (source.Width > 1400) {
                int width = (int)source.Width / 3;
                int height = (int)source.Height / 3;
                var margin = 0;
                var rect = new Rect(margin, margin, width - margin * 2, height - margin * 2);

                var group = new DrawingGroup();
                RenderOptions.SetBitmapScalingMode(group, BitmapScalingMode.HighQuality);
                group.Children.Add(new ImageDrawing(source, rect));

                var drawingVisual = new DrawingVisual();
                using (var drawingContext = drawingVisual.RenderOpen())
                    drawingContext.DrawDrawing(group);

                var resizedImage = new RenderTargetBitmap(
                    width, height,         // Resized dimensions
                    96, 96,                // Default DPI values
                    PixelFormats.Default); // Default pixel format
                resizedImage.Render(drawingVisual);

                image = BitmapFrame.Create(resizedImage);
            } else {
                image = BitmapFrame.Create(source);
            }
            
            
            JObject authentication = await authenticate();
            var status = authentication.GetValue("status");
            string session = string.Empty;
            if (status != null && status.ToString() == "success") {
                session = authentication.GetValue("session").ToString();

                JObject imagesubmission = await submitImage(image, session);

                string subid = string.Empty;
                string jobid = string.Empty;
                if (imagesubmission.GetValue("status") != null && imagesubmission.GetValue("status").ToString() == "success") {
                    subid = imagesubmission.GetValue("subid").ToString();

                    var attempts = 0;
                    while (true) {
                        
                        JObject submissionstatus = await getSubmissionStatus(subid);

                        JArray jobids;
                        jobids = (JArray)submissionstatus.GetValue("jobs");
                        if (jobids.Count > 0) {                            
                            jobid = jobids.First.ToString();
                            if(jobid != "") {
                                break;
                            }
                            
                        }
                        await Task.Delay(1000);
                        attempts++;
                        if(attempts > 30) {
                            break;
                        }
                    };
                    
                    if(!string.IsNullOrWhiteSpace(jobid)) {
                        attempts = 0;
                        string jobstatus = string.Empty;
                        while (true) {
                            JObject ojobstatus = await getJobStatus(jobid);
                            jobstatus = ojobstatus.GetValue("status").ToString();

                            if ((jobstatus == "failure") || (jobstatus == "success")) {
                                break;
                            }
                            await Task.Delay(1000);
                            attempts++;
                            if (attempts > 60) {
                                break;
                            }
                        };

                        if(jobstatus == "success") {
                            JObject job = await getJobInfo(jobid);
                            JobResult jobinfo = job.ToObject<JobResult>();

                            result.Dec = jobinfo.calibration.dec;
                            result.Orientation = jobinfo.calibration.orientation;
                            result.Pixscale = jobinfo.calibration.pixscale;
                            result.Ra = jobinfo.calibration.ra;
                            result.Radius = jobinfo.calibration.radius;

                            result.SolvedImage = await getJobImage(jobid);         
                        }
                    }
                }
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
