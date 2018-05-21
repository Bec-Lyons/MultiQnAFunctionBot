using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.CognitiveServices.QnAMaker;
using Microsoft.Bot.Connector;

// For more information about this template visit http://aka.ms/azurebots-csharp-qnamaker
[Serializable]
public class RootDialog :  IDialog<object>
{

    private const string WelcomeMessage = "Hi. I'm a QnA Bot with multiple QnAMaker knowledge bases.";
        private const string MenuSelectionMessage = "Which knowledge base do you want to query?";

        //Variables for QnAMaker knowledge base #1
        private const string KB1Name = "Knowledge Base 1";
        private const string KB1WelcomeMessage = "You are now asking questions about " + KB1Name;

        //Variables for QnAMaker knowledge base #2
        private const string KB2Name = "Knowledge Base 2";
        private const string KB2WelcomeMessage = "You are now asking questions about " + KB2Name;

    public async Task StartAsync(IDialogContext context)
    {
        /* Wait until the first message is received from the conversation and call MessageReceviedAsync 
         *  to process that message. */
        context.Wait(this.MessageReceivedAsync);
    }

    private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
    {
        /* When MessageReceivedAsync is called, it's passed an IAwaitable<IMessageActivity>. To get the message,
         *  await the result. */
        PromptDialog.Choice(context, this.AfterMenuSelection, new List<string>() { KB1Name, KB2Name }, MenuSelectionMessage);
   
    }

    private async Task AfterMenuSelection (IDialogContext context, IAwaitable<string> result) {
        var optionSelected = await result;
        
        var qnaAuthKey = GetSetting("QnAAuthKey");
        var qnaKBId1 = Utils.GetAppSetting("QnAKnowledgebaseId");
        var qnaKBId2 = Utils.GetAppSetting("QnAKnowledgebaseId2");
        var endpointHostName = Utils.GetAppSetting("QnAEndpointHostName");

        // QnA Subscription Key and KnowledgeBase Id null verification
        if (!string.IsNullOrEmpty(qnaAuthKey) && !string.IsNullOrEmpty(qnaKBId1) && !string.IsNullOrEmpty(qnaKBId2))
        {
             switch (optionSelected)
            {
                //remove keys and put them in config settings
                case KB1Name:
                    await context.PostAsync(KB1WelcomeMessage);
                    context.Call(new QnADialog(qnaAuthKey, qnaKBId1, endpointHostName), ResumeAfterOptionDialogAsync);

                    break;

                case KB2Name:
                    await context.PostAsync(KB2WelcomeMessage);
                    context.Call(new QnADialog(qnaAuthKey, qnaKBId2, endpointHostName), ResumeAfterOptionDialogAsync);

                    break;
            }
        }
        else
        {
            await context.PostAsync("Please set QnAKnowledgebaseId, QnAAuthKey and QnAEndpointHostName (if applicable) in App Settings. Learn how to get them at https://aka.ms/qnaabssetup.");
        }
    }

    private async Task ResumeAfterOptionDialogAsync(IDialogContext context, IAwaitable<object> result)
    {
        PromptDialog.Choice(context, this.AfterMenuSelection, new List<string>() { KB1Name, KB2Name }, MenuSelectionMessage);
    }

    public static string GetSetting(string key)
    {
        var value = Utils.GetAppSetting(key);
        if (String.IsNullOrEmpty(value) && key == "QnAAuthKey")
        {
            value = Utils.GetAppSetting("QnASubscriptionKey"); // QnASubscriptionKey for backward compatibility with QnAMaker (Preview)
        }
        return value;
    }
}


// Dialog for QnAMaker GA service
[Serializable]
public class QnADialog : QnAMakerDialog
{
    // Go to https://qnamaker.ai and feed data, train & publish your QnA Knowledgebase.
    // Parameters to QnAMakerService are:
    // Required: qnaAuthKey, knowledgebaseId, endpointHostName
    // Optional: defaultMessage, scoreThreshold[Range 0.0 – 1.0]

    
    private const string unconfidentMessage = "Hmmmm I'm not too sure about that one.";
    private const double scoreThreshold = 0.5;
    private const int topAnswerCount = 1;

    public QnADialog(string authKey, string kbID, string hostName) : base(new QnAMakerService(new QnAMakerAttribute(authKey, kbID, unconfidentMessage, scoreThreshold, topAnswerCount,hostName)))
    {
    }

    // Override to log matched Q&A before ending the dialog
    protected override async Task DefaultWaitNextMessageAsync(IDialogContext context, IMessageActivity message, QnAMakerResults results)
    {
        if (results.Answers == null || results.Answers.Count == 0)
        {
            await base.DefaultWaitNextMessageAsync(context, message, results);
        }
    }
}