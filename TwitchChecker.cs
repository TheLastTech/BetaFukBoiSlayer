using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Models.v5.Streams;
using TwitchLib.Api.Models.v5.Users;

namespace FuckNinja
{
    public class TwitchChecker
    {
        private readonly List<UserSnapshot> ScrapedUsers;
        private readonly string UsernameOfTarget;
        private readonly TwitchAPI Api;
        private readonly string DumpPath;

        public TwitchChecker(string UsernameTarget, string ClientID, string Accesstoken)
        {
          
            UsernameOfTarget = UsernameTarget;
            DumpPath = $"{UsernameOfTarget}JsonDump.json";
            ScrapedUsers = new List<UserSnapshot>();
            if (File.Exists(DumpPath))
            {
                ScrapedUsers.AddRange(JsonConvert.DeserializeObject<List<UserSnapshot>>(File.ReadAllText(DumpPath)));
            }
            Api = new TwitchAPI();

            Api.Settings.ClientId = ClientID;
            Api.Settings.AccessToken = Accesstoken;
        }

        public async Task Run()
        {
            User UserX = (await Api.Users.v5.GetUserByNameAsync(UsernameOfTarget)).Matches.FirstOrDefault();
            if (UserX == null)
            {
                return;
            }

            StreamByUser ChannelX = (await Api.Streams.v5.GetStreamByUserAsync(UserX.Id));
            Console.WriteLine($"{UserX.DisplayName} has {ChannelX.Stream.Viewers}  viewers");
            Twitcher Target = await DownloadChatters();
            Console.WriteLine($"{UserX.DisplayName} has {ChannelX.Stream.Viewers - Target.Chatters.Viewers.Count}  unregistered viewers watching");
            Target.Chatters.Viewers.AsParallel().ForAll(ProbablyARobot =>
            {
                CheckUsershit(ProbablyARobot).GetAwaiter().GetResult();
            });
          
        }
        public void PrintSuspectDateCounts()
        {
            Dictionary<string, int> CreationCount = new Dictionary<string, int>();
            ScrapedUsers.ForEach(U =>
            {
                string DateKey = U.User.CreatedAt.ToShortDateString();
                if (!CreationCount.ContainsKey(DateKey)) CreationCount.Add(DateKey, 0);
                CreationCount[DateKey]++;
            });
            string[] SusDates = CreationCount.Keys.Where(A => CreationCount[A] > 50).OrderBy(A=>CreationCount[A]).ToArray();
            int TotalSusDates = 0;
      
            foreach (var ShortDate in SusDates)
            {
                Console.WriteLine($"{CreationCount[ShortDate]} Accounts were Created on {ShortDate}");
                TotalSusDates += CreationCount[ShortDate];
            }
            Console.WriteLine($"Total Suspect Accounts : {TotalSusDates}");

        }

        private void RecordBannedAccount(string username)
        {
            Monitor.Enter(Api);
            try
            {
                File.AppendAllText($"{UsernameOfTarget}BannedUsersInChat.txt", $"{username}{Environment.NewLine}");
            }
            catch (Exception Err)
            {
            }
            finally
            {
                Monitor.Exit(Api);
            }
        }

        private async Task<Twitcher> DownloadChatters()
        {
            using (WebClient Client = new WebClient())
            {
                string Json = await Client.DownloadStringTaskAsync($"https://tmi.twitch.tv/group/user/{UsernameOfTarget}/chatters");
                return JsonConvert.DeserializeObject<Twitcher>(Json);
            }
        }

        private async Task CheckUsershit(string Username)
        {
            try
            {
                User UserX = (await Api.Users.v5.GetUserByNameAsync(Username)).Matches.FirstOrDefault();
                if (UserX == null)
                {
                    RecordBannedAccount(Username);
                    return;
                }
                var UserFollows = await Api.Users.v5.GetUserFollowsAsync(UserX.Id);
                //    Console.WriteLine($"{UserX.Name} is being looked at and his {UserFollows.Total} Friends");
                AddScraping(UserFollows, UserX);
            }
            catch (Exception A)
            {
                //   Console.WriteLine(A);
                Task.Delay(10000).GetAwaiter().GetResult();
                await CheckUsershit(Username);
            }
        }

        private void AddScraping(UserFollows Following, User User_)
        {
            Task.Run(() =>
            {
                Monitor.Enter(ScrapedUsers);
                try
                {
                    ScrapedUsers.Add(new UserSnapshot()
                    {
                        Following = Following,
                        User = User_
                    });
                    if (ScrapedUsers.Count % 1000 == 0)
                    {
                        File.WriteAllText(DumpPath, JsonConvert.SerializeObject(ScrapedUsers));
                        Console.WriteLine("Total Done: " + ScrapedUsers.Count);
                    }
                }
                catch (Exception Err)
                {
                }
                finally
                {
                    Monitor.Exit(ScrapedUsers);
                }
            });
        }
    }
}