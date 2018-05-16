# BetaFukBoiSlayer
A tool to help the public slay twitch stream view botters.
        Sample Usage
		```c#
		
		private static void Main(string[] args)
        {
            string ClientID = ""; //get from creating a twitch app
			string AccessToken = ""; //https://twitchtokengenerator.com
			TwitchChecker B =  new TwitchChecker("dakotaz", ClientID, Accesstoken);
            async void Runner()
            {
        		await B.Run();
                B.PrintSuspectDateCounts();

            }
            Runner();
            Task.Delay(-1).GetAwaiter().GetResult();
        }

		```

		Musical Inspiration https://www.youtube.com/watch?v=j6PMblM4L1o

I was paid in teemo souls.