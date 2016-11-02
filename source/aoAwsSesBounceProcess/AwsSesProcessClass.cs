using System;
using System.Collections.Generic;
using System.Text;
using Contensive.BaseClasses;
using Amazon.SQS;
using Amazon.SQS.Model;


namespace Contensive.Addons.aoEmailBounce
{
    /// <summary>
    /// handle Amazon SES email bounces setup through SNS and SQS
    /// - SNS is configured to use a topic named sesbouncenotify-(appname)
    /// - SQS is configured with a queue named sesbouncequeue-(appname)
    /// - SES has email addresses and domains configured to notify the SNS sesbouncenotify-(appname)
    /// </summary>
    public class AwsSesProcessClass : Contensive.BaseClasses.AddonBaseClass
    {
        //==========================================================================================
        /// <summary>
        /// addon method
        /// </summary>
        /// <param name="cp"></param>
        /// <returns></returns>
        public override object Execute(Contensive.BaseClasses.CPBaseClass cp)
        {
            string returnEmpty = "";
            DateTime rightNowDate = DateTime.Now;
            string logDatePart = rightNowDate.Year + rightNowDate.Month.ToString().PadLeft(2) + rightNowDate.Day.ToString().PadLeft(2);
            try
            {
                // site properties
                //
                //cp.Utils.AppendLog("emailBounce\\" + logDatePart + ".log", "start");
                //
                const string spAwsSecretAccessKey = "AWS Secret Access Key";
                const string spAwsAccessKeyId = "AWS Access Key Id";
                const string spAwsSQSBounceEmailQueueEndpoint = "AWS SQS Bounce Email Queue Endpoint";
                //
                bool awsAllowBounceProcess = cp.Site.GetBoolean("AWS SES Allow Bounce Process");
                if (awsAllowBounceProcess) 
                {
                    string awsSecretAccessKey = cp.Site.GetText(spAwsSecretAccessKey);
                    string awsAccessKeyId = cp.Site.GetText(spAwsAccessKeyId);
                    string awsSQSBounceEmailQueueEndpoint = cp.Site.GetText(spAwsSQSBounceEmailQueueEndpoint);
                    //
                    //string awsServiceUrl = "https://sqs.us-east-1.amazonaws.com";
                    //
                    //AmazonSQSConfig sqsConfig = new AmazonSQSConfig();
                    //sqsConfig.ServiceURL = awsServiceUrl;
                    AmazonSQSClient sqsClient = new AmazonSQSClient(awsAccessKeyId, awsSecretAccessKey, Amazon.RegionEndpoint.USEast1);
                    ReceiveMessageRequest receiveMessageRequest = new ReceiveMessageRequest();
                    receiveMessageRequest.QueueUrl = awsSQSBounceEmailQueueEndpoint;
                    receiveMessageRequest.MaxNumberOfMessages = 10;
                    while (true)
                    {
                        ReceiveMessageResponse receiveMessageResponse = sqsClient.ReceiveMessage(receiveMessageRequest);
                        if (receiveMessageResponse.Messages.Count == 0) break;                //
                        foreach (Message msg in receiveMessageResponse.Messages)
                        {
                            // First, convert the Amazon SNS message into a JSON object.
                            AmazonSqsNotification notification = Newtonsoft.Json.JsonConvert.DeserializeObject<AmazonSqsNotification>(msg.Body);
                            if (notification.Type == "Notification")
                            {
                                // Now access the Amazon SES bounce notification.
                                AmazonSesBounceNotification message = Newtonsoft.Json.JsonConvert.DeserializeObject<AmazonSesBounceNotification>(notification.Message);
                                if (message.NotificationType == "Bounce")
                                {
                                    string bounceMsg = message.Bounce.Timestamp.ToString() + " AWS email bounce notification, type: " + message.Bounce.BounceType;
                                    if (!string.IsNullOrEmpty(message.Bounce.BounceSubType))
                                    {
                                        bounceMsg += ", " + message.Bounce.BounceSubType;
                                    }
                                    switch (message.Bounce.BounceType)
                                    {
                                        case "Transient":
                                            // Remove all recipients that generated a permanent bounce 
                                            // or an unknown bounce.
                                            foreach (var recipient in message.Bounce.BouncedRecipients)
                                            {
                                                transientFail(cp, recipient.EmailAddress, bounceMsg);
                                            }
                                            break;
                                        default:
                                            // Remove all recipients that generated a permanent bounce 
                                            // or an unknown bounce.
                                            foreach (var recipient in message.Bounce.BouncedRecipients)
                                            {
                                                permanentFail(cp, recipient.EmailAddress, bounceMsg);
                                            }
                                            break;
                                    }
                                }
                            }
                            var deleteMessageRequest = new DeleteMessageRequest();
                            deleteMessageRequest.QueueUrl = awsSQSBounceEmailQueueEndpoint;
                            deleteMessageRequest.ReceiptHandle = msg.ReceiptHandle;
                            sqsClient.DeleteMessage(deleteMessageRequest);
                        }
                    }
                    //
                    // transient bounces beyond the grace period - convert to permanent failures
                    //
                    CPCSBaseClass cs = cp.CSNew();
                    if (cs.Open("email bounce list", "(transient=1)and(transientFixDeadline<" + cp.Db.EncodeSQLDate( DateTime.Now ) + ")"))
                    {
                        do
                        {
                            permanentFail(cp, cs.GetText("name"),  DateTime.Now.ToString() + " converted from transient to permanent because grace period past with no action, original failure[" + cs.GetText("detail") + "]" );
                            cs.GoNext();
                        } while (cs.OK());
                    }
                    cs.Close();
                }
                //
                //cp.Utils.AppendLog("emailBounce\\" + logDatePart + ".log", "exit");
                //
            }
            catch (Exception ex)
            {
                cp.Site.ErrorReport(ex);
            }
            return returnEmpty;
        }
        //==========================================================================================
        /// <summary>
        /// handle a permanent fail
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="emailAddress"></param>
        private void permanentFail(CPBaseClass cp, string emailAddress , string bounceMsg )
        {
            //
            // clear allowBulkEmail
            //
            cp.Db.ExecuteSQL("update ccmembers set allowBulkEmail=0 where email=" + cp.Db.EncodeSQLText(emailAddress));
            //
            // add or update email bounce list
            //
            CPCSBaseClass cs = cp.CSNew();
            if (cs.Open("Email Bounce List", "name=" + cp.Db.EncodeSQLText(emailAddress)))
            {
                cs.SetField("details", bounceMsg);
                cs.SetField("transient", "0");
            }
            else 
            {
                cs.Close();
                if (cs.Insert("Email Bounce List"))
                {
                    cs.SetField("name", emailAddress);
                    cs.SetField("details", bounceMsg);
                    cs.SetField("transient", "0");
                }
            }
            cs.Close();
            //
            // add to server's block list, "(programfiles)\config\SMTPBlockList_(appName).txt", vbcrlf + emailAddress + vbTab + dateTime
            //
            string filename = cp.Site.PhysicalInstallPath + "\\config\\SMTPBlockList_" + cp.Site.Name + ".txt";
            string blockList = cp.File.Read(filename);
            cp.File.Save(filename, blockList + "\n\r" + emailAddress + "\t" + DateTime.Now.ToString());
        }
        //==========================================================================================
        /// <summary>
        /// handle a transient fail
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="emailAddress"></param>
        private void transientFail(CPBaseClass cp, string emailAddress, string bounceMsg)
        {
            const string spAWSGracePeriod = "AWS SES Transient Email Grace Period";
            //
            // do not clear allowBulkEmail
            // add or update email bounce list
            //
            CPCSBaseClass cs = cp.CSNew();
            if (cs.Open("Email Bounce List", "(name=" + cp.Db.EncodeSQLText(emailAddress) + ")"))
            {
                if (cs.GetBoolean("transient")) 
                {
                    // 
                    // previous transient failure
                    //
                    if (DateTime.Now > cs.GetDate("transientFixDeadline") ) 
                    {
                        //
                        // past deadline, covert to permanent fail
                        //
                        cs.Close();
                        permanentFail(cp, emailAddress, bounceMsg );
                    } else {
                        //
                        // not past deadline, update details 
                        //
                        cs.SetField("details", bounceMsg);
                    }
                } else {
                    //
                    // previous permanent failure - do nothing
                    //
                }
            }
            else
            {
                //
                // no previous failures, add them
                //
                cs.Close();
                if (cs.Insert("Email Bounce List"))
                {
                    cs.SetField("name", emailAddress);
                    cs.SetField("details", bounceMsg);
                    cs.SetField("transient", "1");
                    cs.SetField("transientFixDeadline", DateTime.Now.AddDays( cp.Site.GetInteger( spAWSGracePeriod )).ToShortDateString());
                }
            }
            cs.Close();
        }
        //==========================================================================================
        /// <summary>
        /// Add this user to the email review list
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="emailAddress"></param>
        private void addToReviewList(CPBaseClass cp, AmazonSesBouncedRecipient recipient)
        {

        }
        /// <summary>Represents the bounce or complaint notification stored in Amazon SQS.</summary>
        class AmazonSqsNotification
        {
            public string Type { get; set; }
            public string Message { get; set; }
        }

        /// <summary>Represents an Amazon SES bounce notification.</summary>
        class AmazonSesBounceNotification
        {
            public string NotificationType { get; set; }
            public AmazonSesBounce Bounce { get; set; }
        }
        /// <summary>Represents meta data for the bounce notification from Amazon SES.</summary>
        class AmazonSesBounce
        {
            public string BounceType { get; set; }
            public string BounceSubType { get; set; }
            public DateTime Timestamp { get; set; }
            public List<AmazonSesBouncedRecipient> BouncedRecipients { get; set; }
        }
        /// <summary>Represents the email address of recipients that bounced
        /// when sending from Amazon SES.</summary>
        class AmazonSesBouncedRecipient
        {
            public string EmailAddress { get; set; }
        }
    }
}
