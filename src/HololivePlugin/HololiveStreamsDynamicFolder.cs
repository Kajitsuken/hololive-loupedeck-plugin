namespace Loupedeck.HololivePlugin
{
    using Loupedeck;
    using System;
    using System.Diagnostics;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Text.Json;
    using System.Drawing;
    using System.Threading;

    public class HololiveStreamsDynamicFolder : PluginDynamicFolder
    {
        private readonly HttpClient httpClient = new HttpClient();
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private Dictionary<String, (String TalentId, String Talent, String Status)> streams = [];
        private List<String> timeOrderedStreamIds = [];
        private String holodexApiKey;
        private Boolean folderOpen = false;
        private Boolean hideHolostars;
        private Boolean hideGuestAppearances;

        public HololiveStreamsDynamicFolder()
        {
            this.DisplayName = "Hololive Streams";
            this.GroupName = "Folders";
        }

        // Populates the encoder area with the back button and the page controls
        public override PluginDynamicFolderNavigation GetNavigationArea(DeviceType _) => PluginDynamicFolderNavigation.EncoderArea;

        // Sets the folder image
        public override BitmapImage GetButtonImage(PluginImageSize imageSize) => EmbeddedResources.ReadImage("Loupedeck.HololivePlugin.Resources.Images.hololive_icon.png");

        // Sets up the buttons
        public override IEnumerable<String> GetButtonPressActionNames(DeviceType deviceType)
        {
            var buttonNames = new List<String>();
            foreach (var streamId in this.timeOrderedStreamIds)
            {
                buttonNames.Add(this.CreateCommandName(streamId));
            }

            return buttonNames;
        }

        // Run on button press. Opens the talent's stream in browser.
        public override void RunCommand(String actionParameter)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = $"https://youtube.com/watch?v={actionParameter}",
                UseShellExecute = true
            };

            Process.Start(processInfo);
        }

        // Sets the image for each button
        public override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            if (this.streams.TryGetValue(actionParameter, out var streamInfo))
            {
                var filePath = $"{this.Plugin.GetPluginDataDirectory()}/images/{streamInfo.TalentId}.png";
                var image = System.IO.File.Exists(filePath) ? BitmapImage.FromFile(filePath) : null;
                if (image == null)
                {
                    return null;
                }
                else if (this.streams[actionParameter].Status == "live")
                {
                    return image;
                }
                else
                {
                    return image.MakeGrayscale();
                }
            } else
            {
                return null;
            }
        }

        // Sets the display name. Usually not used as images are present.
        public override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            return this.streams.TryGetValue(actionParameter, out var value) ? value.Talent : null;
        }

        // Loop which fetches new streams every minute if the folder is in active use.
        private async Task GetStreamsTask(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested) {
                PluginLog.Info("Fetching Hololive streams...");
                if (this.folderOpen)
                {
                    await this.GetStreams(cancellationToken);
                }

                await Task.Delay(60000, cancellationToken);
            }
        }

        // Main logic of the plugin which fetches all live streams.
        private async Task GetStreams(CancellationToken cancellationToken)
        {
            var url = "https://holodex.net/api/v2/live?org=Hololive&status=live,upcoming&max_upcoming_hours=1&sort=available_at&order=desc";
            var streams = new Dictionary<String, (String TalentId, String Talent, String Status)>();
            var streamIdsLiveOrdered = new List<String>();
            var streamIdsUpcomingOrdered = new List<String>();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("X-APIKEY", this.holodexApiKey);
                using var response = await this.httpClient.SendAsync(request, cancellationToken);
                var responseText = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(responseText);
                foreach (var stream in data.EnumerateArray())
                {
                    var status = stream.GetProperty("status").ToString();
                    var channel = stream.GetProperty("channel");
                    var org = channel.TryGetProperty("org", out var orgValue) ? orgValue.ToString() : null;
                    var suborg = channel.TryGetProperty("suborg", out var suborgValue) ? suborgValue.ToString() : null;

                    // Filter streams based on the JSON config settings
                    if ((this.hideGuestAppearances && org != "Hololive") || (this.hideHolostars && suborg.Contains("HOLOSTARS")))
                    {
                        continue;
                    }

                    var talentId = channel.GetProperty("id").ToString();
                    var talentName = channel.GetProperty("english_name").ToString();
                    var streamId = stream.GetProperty("id").ToString();
                    
                    var imageUrl = channel.GetProperty("photo").ToString();
                    await this.DownloadAndSaveImage(imageUrl, talentId, cancellationToken);

                    streams[streamId] = (talentId, talentName, status);
                    if (status == "live")
                    {
                        streamIdsLiveOrdered.Add(streamId);
                    } else
                    {
                        streamIdsUpcomingOrdered.Add(streamId);
                    }
                }

                // Order of upcoming streams needs to be reversed so streams starting sooner are first
                streamIdsUpcomingOrdered.Reverse();
                var streamIdsOrdered = streamIdsLiveOrdered.Concat(streamIdsUpcomingOrdered).ToList();

                if (!streamIdsOrdered.SequenceEqual(this.timeOrderedStreamIds))
                {
                    this.streams = streams;
                    this.timeOrderedStreamIds = streamIdsOrdered;
                    this.ButtonActionNamesChanged();
                    this.EncoderActionNamesChanged();

                    // Refresh all button images to account for streams switching from upcoming to live
                    foreach (var id in streamIdsOrdered)
                    {
                        this.CommandImageChanged(id);
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error("Couldn't get new streams.");
            }
        }

        // Downloads a talent's profile picture to be used as their respective button image.
        private async Task DownloadAndSaveImage(String imageUrl, String talentId, CancellationToken cancellationToken)
        {
            var imageDir = $"{this.Plugin.GetPluginDataDirectory()}/images";
            System.IO.Directory.CreateDirectory(imageDir);
            var imagePath = $"{imageDir}/{talentId}.png";
            if (System.IO.File.Exists(imagePath))
            {
                return;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, imageUrl);
            using var response = await this.httpClient.SendAsync(request, cancellationToken);
            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            using var ms = new MemoryStream(imageBytes);
            using var bitmap = new Bitmap(ms);
            using var resizedBitmap = new Bitmap(bitmap, new Size(256, 256));
            resizedBitmap.Save(imagePath, System.Drawing.Imaging.ImageFormat.Png);
        }

        public override Boolean Load()
        {
            // Load config values from JSON file
            var jsonContent = File.ReadAllText($"{this.Plugin.GetPluginDataDirectory()}/config.json");
            var json = JsonSerializer.Deserialize<JsonElement>(jsonContent);
            this.holodexApiKey = json.GetProperty("api-key").GetString();
            this.hideGuestAppearances = json.TryGetProperty("hideGuestAppearances", out var hideGuestAppearances) ? hideGuestAppearances.GetBoolean() : false;
            this.hideHolostars = json.TryGetProperty("hideHolostars", out var hideHolostars) ? hideHolostars.GetBoolean() : false;

            // Start loop
            Task.Run(() => this.GetStreamsTask(this.cancellationTokenSource.Token));
            return true;
        }

        public override Boolean Activate()
        {
            // Always refresh streams when the folder is entered
            Task.Run(() => this.GetStreams(this.cancellationTokenSource.Token)).Wait();
            this.folderOpen = true;
            return true;
        }

        public override Boolean Deactivate()
        {
            this.folderOpen = false;
            return true;
        }
    }
}
