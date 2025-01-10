using OpenCvSharp;
using videomasking.Services.Contracts;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace videomasking.Services
{
    public class VideoProcessingService : IVideoProcessingService
    {
        public async Task StreamProcessedVideo(HttpResponse response)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "sample.mp4");

            using (var videoCapture = new VideoCapture(filePath))
            {
                if (!videoCapture.IsOpened())
                    throw new FileNotFoundException("Video file not found!");

                // Postavke za HTTP response
                response.ContentType = "multipart/x-mixed-replace; boundary=frame";

                using (var frame = new Mat())
                {
                    while (videoCapture.Read(frame))
                    {
                        if (frame.Empty())
                            break;

                        // Obrada framea (npr. zamagljivanje lica)
                        var processedFrame = ApplyMasking(frame);

                        // Kompresija framea u JPEG format
                        byte[] jpegData = processedFrame.ToBytes(".jpg");

                        // Slanje frejma klijentu
                        await response.Body.WriteAsync(GenerateMJPEGFrame(jpegData));
                    }
                }
            }
        }

        private Mat ApplyMasking(Mat frame)
        {
            // Konvertovanje frame-a u bajtove
            byte[] imageBytes = frame.ToBytes(".jpg");

            // Pozivanje Python skripte za detekciju
            var pythonPath = "python";
            var scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "mediapipe_detect.py");

            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = scriptPath,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(processInfo);

            // Slanje slike kroz standardni ulaz Python skripti
            using (var writer = process.StandardInput.BaseStream)
            {
                writer.Write(imageBytes, 0, imageBytes.Length);
                writer.Flush();
            }

            // Čitanje izlaza iz Python skripte
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Obrada rezultata
            var detections = ParseDetections(output);

            // Primena zamagljivanja na detektovane regije
            foreach (var (x1, y1, x2, y2) in detections)
            {
                var roi = new Rect(x1, y1, x2 - x1, y2 - y1);
                var region = new Mat(frame, roi);
                Cv2.GaussianBlur(region, region, new OpenCvSharp.Size(51, 51), 30);
            }

            return frame;
        }



        private List<(int, int, int, int)> ParseDetections(string output)
        {
            var detections = new List<(int, int, int, int)>();

            try
            {
                // Filtrirajte ulaz (provjera da li ima detekcija)
                output = output.Trim(new char[] { '[', ']', ' ' });
                if (string.IsNullOrEmpty(output) || output == "[]")
                    return detections;
                Console.WriteLine($"Python output: {output}");

                var boxes = output.Split("),");
                foreach (var box in boxes)
                {
                    var cleanBox = box.Trim(new char[] { '(', ')', ' ' });
                    var coords = cleanBox.Split(',');

                    // Provjera validnosti koordinate
                    if (coords.Length == 4 &&
                        int.TryParse(coords[0], out int x1) &&
                        int.TryParse(coords[1], out int y1) &&
                        int.TryParse(coords[2], out int x2) &&
                        int.TryParse(coords[3], out int y2))
                    {
                        detections.Add((x1, y1, x2, y2));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing detections: {ex.Message}");
            }

            return detections;
        }


        private byte[] GenerateMJPEGFrame(byte[] jpegData)
        {
            // Generiše MJPEG format za HTTP streaming
            using (var stream = new MemoryStream())
            {
                var writer = new StreamWriter(stream);
                writer.Write("--frame\r\n");
                writer.Write("Content-Type: image/jpeg\r\n");
                writer.Write($"Content-Length: {jpegData.Length}\r\n\r\n");
                writer.Flush();

                stream.Write(jpegData, 0, jpegData.Length);
                writer.Write("\r\n");
                writer.Flush();

                return stream.ToArray();
            }
        }
    }
}