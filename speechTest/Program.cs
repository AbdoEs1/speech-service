using System;
using System.Net.Http;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;
using System.Threading;
using Microsoft.CognitiveServices.Speech;

namespace speechTest
{
    class Program
    {
        private static string SUBSCRIPTION_KEY = "SUBSCRIPTION_KEY";
        private static string TOKENFETCHURI = "TOKENFETCHURI";
        private static string REGION = "REGION";
        private static readonly string FILE_NAME = @"FILE_NAME";
        private static readonly string HOST = "HOST";

        static async Task Main(string[] args)
        {
            
            Console.WriteLine("Text-to-speech");
            await TextToSpeech();
            Console.WriteLine("Speech-to-text.");
            Console.ReadLine();
            RecognizeSpeechAsync().Wait();
            Console.WriteLine("Please press a key to continue.");
            Console.ReadLine();
        }

        public static async Task TextToSpeech()
        {
            // Prompts the user to input text for TTS conversion
            Console.Write("What would you like to convert to speech? ");
            string text = Console.ReadLine();

            // Gets an access token
            string accessToken;
            Console.WriteLine("Attempting token exchange. Please wait...\n");

            // Add your subscription key here
            // If your resource isn't in WEST US, change the endpoint
            Authentication auth = new Authentication(TOKENFETCHURI, SUBSCRIPTION_KEY);
            try
            {
                accessToken = await auth.FetchTokenAsync().ConfigureAwait(false);
                Console.WriteLine("Successfully obtained an access token. \n");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to obtain an access token.");
                Console.WriteLine(ex.ToString());
                Console.WriteLine(ex.Message);
                return;
            }
            

            string body = @"<speak version='1.0' xmlns='https://www.w3.org/2001/10/synthesis' xml:lang='en-US'>
              <voice name='Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)'>" +
              text + "</voice></speak>";

            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage())
                {
                    // Set the HTTP method
                    request.Method = HttpMethod.Post;
                    // Construct the URI
                    request.RequestUri = new Uri(HOST);
                    // Set the content type header
                    request.Content = new StringContent(body, Encoding.UTF8, "application/ssml+xml");
                    // Set additional header, such as Authorization and User-Agent
                    request.Headers.Add("Authorization", "Bearer " + accessToken);
                    request.Headers.Add("Connection", "Keep-Alive");
                    // Update your resource name
                    request.Headers.Add("User-Agent", "productSpeech");
                    request.Headers.Add("X-Microsoft-OutputFormat", "riff-24khz-16bit-mono-pcm");
                    // Create a request
                    Console.WriteLine("Calling the TTS service. Please wait... \n");
                    using (var response = await client.SendAsync(request).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        // Asynchronously read the response
                        using (var dataStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            Console.WriteLine("Your speech file is being written to file...");
                            using (var fileStream = new FileStream(@"sample.wav", FileMode.Create, FileAccess.Write, FileShare.Write))
                            {
                                await dataStream.CopyToAsync(fileStream).ConfigureAwait(false);
                                fileStream.Close();
                            }
                            Console.WriteLine("\nYour file is ready. Press any key to exit.");
                        }
                        
                        playSound(FILE_NAME);


                    }
                }
            }
        }

        public static void playSound(string fileName)
        {
            using (var waveOut = new WaveOutEvent())
            using (var wavReader = new WaveFileReader(fileName))
            {
                waveOut.Init(wavReader);
                waveOut.Play();
                while (waveOut.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(100);
                }
                waveOut.Dispose();
            }
        }
        
        public static async Task RecognizeSpeechAsync()
        {
            var config = SpeechConfig.FromSubscription(SUBSCRIPTION_KEY,REGION);

            // Creates a speech recognizer.
            using (var recognizer = new SpeechRecognizer(config))
            {
                Console.WriteLine("Say something...");

                var result = await recognizer.RecognizeOnceAsync();

                // Checks result.
                if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    Console.WriteLine($"We recognized: {result.Text}");
                }
                else if (result.Reason == ResultReason.NoMatch)
                {
                    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = CancellationDetails.FromResult(result);
                    Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you update the subscription info?");
                    }
                }
            }
        }

    }
}
