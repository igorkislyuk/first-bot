using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;

namespace FirstBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                Activity reply = ExecuteCommandWith(activity);

                await connector.Conversations.ReplyToActivityAsync(reply);
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }

        private Activity ExecuteCommandWith(Activity activity)
        {
            Activity reply = DefaultReplyTo(activity);

            var commands = activity.Text.Trim().Split(' ').ToArray();
            if (commands.First().Equals("list"))
            {
                //return list
                using (var db = new SchoolEntities())
                {
                    var firstNames = db.People.Select(p => p.FirstName);

                    //create message
                    var sb = new StringBuilder();
                    foreach (var name in firstNames)
                    {
                        sb.Append(name + "\n");
                    }

                    var resultMessage = sb.ToString();
                    reply = activity.CreateReply(resultMessage);
                }
            }
            else if (commands.First().Equals("rm"))
            {
                if (commands.Length != 2)
                {
                    return DefaultReplyTo(activity);
                }

                var name = commands[1];
                reply = activity.CreateReply($"You are going delete {name}");

                var closureActivity = activity;
                var deleteThread = new Thread(() =>
                {
                    //remove command
                    using (var db = new SchoolEntities())
                    {
                        Person personToDelete = db.People.First(person => person.FirstName == name);

                        db.People.Remove(personToDelete);
                        db.SaveChanges();

                        var connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                        var confirmActivity = activity.CreateReply($"You are delete {name}");

                        connector.Conversations.ReplyToActivityAsync(confirmActivity);
                    }
                });

                deleteThread.Start();
                deleteThread.Join();
            }

            return reply;
        }

        private Activity DefaultReplyTo(Activity activity)
        {
            // calculate something for us to return
            int length = (activity.Text ?? string.Empty).Length;

            // return our reply to the user
            return activity.CreateReply($"You sent {activity.Text} which was {length} characters");
        }
    }
}