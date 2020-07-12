// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.9.2

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Chip.Bots
{
    public class EchoBot : ActivityHandler
    {
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var name = turnContext.Activity.Text;
            if ("help".Equals(name.ToLower()))
            { 
                await turnContext.SendActivityAsync(MessageFactory.Text("Type in a VUMC member name to get people finder info", ""), cancellationToken);
            }
            else{

                //var name = "Ryan Clinton";
                var names = name.Split(" ");
                HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create($"https://peoplefinder.app.vumc.org/index.jsp?action=list&Last={names[1]}&First={names[0]}&IsStaff=on&IsStudent=on&Find=Find");
                myReq.Method = "GET";

                using (HttpWebResponse response = (HttpWebResponse)myReq.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    { //Something went wrong  
                        throw new Exception("Something went wrong");
                    }

                    using (Stream responseStream = response.GetResponseStream())
                    {
                        if (responseStream != null)
                        {
                            using (StreamReader reader = new StreamReader(responseStream))
                            {
                                String responseString = reader.ReadToEnd();
                                String[] tableRows = responseString.Split("<tr>");
                                AdaptiveCard replyCard = ProcessRows(name, tableRows);

                                var reply = new Attachment()
                                {
                                    ContentType = "application/vnd.microsoft.card.adaptive",
                                    Content = JsonConvert.DeserializeObject(replyCard.ToJson())
                                };

                                await turnContext.SendActivityAsync(MessageFactory.Attachment(reply), cancellationToken);
                            }
                        }
                    }
                }
            }
        }


        private static AdaptiveCard ProcessRows(String name, String[] tableRows)
        {
            String schimaVersion = "1.0";
            AdaptiveCard card = new AdaptiveCard(schimaVersion);

            card.Body.Add(new AdaptiveTextBlock
            {
                Text = $"All About {name}",
                Size = AdaptiveTextSize.Large,
                Weight = AdaptiveTextWeight.Bolder
            });

            for (var i = 1; i < tableRows.Length - 1; i++)
            {
                String s = Regex.Replace(tableRows[i], "<.*?>", String.Empty);
                s = Regex.Replace(s, @"\s+", " ");
                s = Regex.Replace(s, "&nbsp;?", String.Empty);

                card.Body.Add(new AdaptiveTextBlock(s));
            }
            return card;
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    var welcomeText = $"Hello and welcome, {member.Name}!";
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText, welcomeText), cancellationToken);
                }
            }
        }
    }
}
