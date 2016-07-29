using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using HtmlAgilityPack;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Utilities;

namespace N3YammerBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        //our base URL for searching the Azure documentation
        private const string searchPath = "https://azure.microsoft.com/en-us/search/documentation/?q=";
        //remove these characters if they show up
        private const string spacer = "â€”";
        //the list of sites to block out in our answer list (free trial, etc.)
        private List<string> blockedAnswerList = new List<string>();

        /// <summary>
        /// Initialize our blocked answer list.
        /// </summary>
        public MessagesController()
        {
            blockedAnswerList.Add("Manage Your Azure Account");
            blockedAnswerList.Add("Azure FREE Trial - Try Azure for free today");
        }
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and search the Azure documentation portal with it.
        /// </summary>
        public async Task<Message> Post([FromBody]Message message)
        {
            if (message.Type == "Message")
            {
                //encode our search term to put into the URL
                var searchTerm = WebUtility.UrlEncode(message.Text);

                //Perform the search
                var html = new HtmlDocument();
                html.LoadHtml(new WebClient().DownloadString(searchPath + searchTerm));

                //pull out the search result <div/> elements of class "wa-searchResult"
                var root = html.DocumentNode;
                var nodes = root.Descendants();
                var searchresults =
                    nodes.Where(n => n.GetAttributeValue("class", "").Equals("wa-searchResult")).ToList();

                //Loop through the results and append our titles + URLs
                var sb = new StringBuilder();
                sb.AppendLine(searchresults.Count + " results: \r\n");
                foreach( var result in searchresults)
                {
                    try
                    {
                        var node = result.ChildNodes["div"].ChildNodes["h4"].ChildNodes["a"];
                        if (node.Attributes.Count == 0) continue;
                        if (string.IsNullOrEmpty(node.InnerText)) continue;

                        var href = node.Attributes["href"].Value;
                        var txt = node.InnerText;

                        //make sure we're not picking up unwanted search results
                        bool containsBlocked = 
                            blockedAnswerList.Any(blockedAnswer => txt.Contains(blockedAnswer));
                        if (containsBlocked) continue;

                        //add our search result to the answer message
                        sb.AppendLine(txt.Replace(spacer, " - "));
                        sb.AppendLine();
                        sb.AppendLine("http://azure.microsoft.com" + href);
                        sb.AppendLine();
                    }
                    catch (Exception e)
                    {
                        sb.AppendLine(e.GetType() + ": " + e.Message);
                        sb.AppendLine();
                    }
                }                

                // return our reply to the user
                return message.CreateReplyMessage(sb.ToString());
            }
            else
            {
                //handle DeleteUserData,UserAddedToConversation, Ping and other system events
                return HandleSystemMessage(message);
            }
        }

        private Message HandleSystemMessage(Message message)
        {
            if (message.Type == "Ping")
            {
                Message reply = message.CreateReplyMessage();
                reply.Type = "Hi! I'm SamIYam, the N3 Yammer bot!";
                return reply;
            }
            else if (message.Type == "DeleteUserData")
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == "BotAddedToConversation")
            {
                Message reply = message.CreateReplyMessage("Hello! I'm SamIYam, the N3 Yammer Bot!");
                reply.Type = "join";
                return reply;
            }
            else if (message.Type == "BotRemovedFromConversation")
            {
            }
            else if (message.Type == "UserAddedToConversation")
            {
                return message.CreateReplyMessage($"Hello, {message.BotUserData}! ");
            }
            else if (message.Type == "UserRemovedFromConversation")
            {
            }
            else if (message.Type == "EndOfConversation")
            {
                return message.CreateReplyMessage("See ya! -SamIYam");
            }

            return null;
        }
    }
}