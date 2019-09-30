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
            cp.Utils.AppendLog( "permanentFailMessages=1" );
            string returnEmpty = "";
            DateTime rightNowDate = DateTime.Now;
            string logDatePart = rightNowDate.Year + rightNowDate.Month.ToString().PadLeft(2) + rightNowDate.Day.ToString().PadLeft(2);
            try
            {
                cp.Utils.AppendLog("permanentFailMessages=2");
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
                    cp.Utils.AppendLog( "permanentFailMessages=3");
                    string awsSecretAccessKey = cp.Site.GetText(spAwsSecretAccessKey);
                    string awsAccessKeyId = cp.Site.GetText(spAwsAccessKeyId);
                    string awsSQSBounceEmailQueueEndpoint = cp.Site.GetText(spAwsSQSBounceEmailQueueEndpoint);
                    //
                    //string awsServiceUrl = "https://sqs.us-east-1.amazonaws.com";
                    //
                    //AmazonSQSConfig sqsConfig = new AmazonSQSConfig();
                    //sqsConfig.ServiceURL = awsServiceUrl;
                    AmazonSQSClient sqsClient = new AmazonSQSClient(awsAccessKeyId, awsSecretAccessKey, Amazon.RegionEndpoint.USEast1);
                    ReceiveMessageRequest receiveMessageRequest = new ReceiveMessageRequest {
                        QueueUrl = awsSQSBounceEmailQueueEndpoint,
                        MaxNumberOfMessages = 10
                    };
                    while (true)
                    {
                        cp.Utils.AppendLog( "permanentFailMessages=4");
                        ReceiveMessageResponse receiveMessageResponse = sqsClient.ReceiveMessage(receiveMessageRequest);
                        if (receiveMessageResponse.Messages.Count == 0) break;                //
                        foreach (Message msg in receiveMessageResponse.Messages)
                        {
                            cp.Utils.AppendLog( "permanentFailMessages=5" + msg.Body);
                            // First, convert the Amazon SNS message into a JSON object.
                            AmazonSqsNotification notification = Newtonsoft.Json.JsonConvert.DeserializeObject<AmazonSqsNotification>(msg.Body);
                            cp.Utils.AppendLog("permanentFailMessages=6" + notification.type);
                            if (notification.type == "Notification")
                            {
                                cp.Utils.AppendLog( "permanentFailMessages=7");
                                // Now access the Amazon SES bounce notification.
                                AmazonSesBounceNotification message = Newtonsoft.Json.JsonConvert.DeserializeObject<AmazonSesBounceNotification>(notification.message);
                                processAmazonSesBounceNotificationMessage(cp, message);
                            } else if (notification.type == null)
                            {
                                cp.Utils.AppendLog( "permanentFailMessages=7");
                                AmazonSesBounceNotification message = Newtonsoft.Json.JsonConvert.DeserializeObject<AmazonSesBounceNotification>(msg.Body);
                                processAmazonSesBounceNotificationMessage(cp, message);
                            }
                            var deleteMessageRequest = new DeleteMessageRequest {
                                QueueUrl = awsSQSBounceEmailQueueEndpoint,
                                ReceiptHandle = msg.ReceiptHandle
                            };
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
                            permanentFail(cp, cs.GetText("name"),  DateTime.Now.ToString() + " converted from transient to permanent because grace period past with no action, original failure[" + cs.GetText("details") + "]" );
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
        //
        private static void processAmazonSesBounceNotificationMessage(CPBaseClass cp, AmazonSesBounceNotification message)
        {
            cp.Utils.AppendLog("permanentFailMessages=8" + message.notificationType);
            if (message.notificationType == "Bounce")
            {
                cp.Utils.AppendLog( "permanentFailMessages=9" + message.notificationType);
                string bounceMsg = message.bounce.timestamp.ToString() + " AWS email bounce notification, type: " + message.bounce.bounceType;
                if (!string.IsNullOrEmpty(message.bounce.bounceSubType))
                {
                    cp.Utils.AppendLog( "permanentFailMessages=10" + bounceMsg);
                    cp.Utils.AppendLog("permanentFailMessages=11, message.Bounce.BouncedRecipients.count [" + message.bounce.bouncedRecipients + "]");
                    bounceMsg += ", " + message.bounce.bounceSubType;
                }
                switch (message.bounce.bounceType)
                {
                    case "Transient":
                        // Remove all recipients that generated a permanent bounce 
                        // or an unknown bounce.
                        foreach (var recipient in message.bounce.bouncedRecipients)
                        {
                            transientFail(cp, recipient.emailAddress, bounceMsg);
                        }
                        break;
                    default:
                        // Remove all recipients that generated a permanent bounce 
                        // or an unknown bounce.
                        foreach (var recipient in message.bounce.bouncedRecipients)
                        {
                            permanentFail(cp, recipient.emailAddress, bounceMsg);
                            cp.Utils.AppendLog( "permanentFailMessages=" + recipient.emailAddress);
                        }
                        break;
                }
            }
        }
        //==========================================================================================
        /// <summary>
        /// handle a permanent fail
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="emailAddress"></param>
        private static void permanentFail(CPBaseClass cp, string emailAddress , string bounceMsg )
        {
            //
            // clear allowBulkEmail
            //
            cp.Db.ExecuteNonQuery("update ccmembers set allowBulkEmail=0 where email=" + cp.Db.EncodeSQLText(emailAddress));
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
            string filename = "\\config\\SMTPBlockList_" + cp.Site.Name + ".txt";
            string blockList = cp.CdnFiles.Read(filename);
            cp.CdnFiles.Save(filename, blockList + "\n\r" + emailAddress + "\t" + DateTime.Now.ToString());
        }
        //==========================================================================================
        /// <summary>
        /// handle a transient fail
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="emailAddress"></param>
        private static void transientFail(CPBaseClass cp, string emailAddress, string bounceMsg)
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
            public string type { get; set; }
            public string message { get; set; }
        }

        /// <summary>Represents an Amazon SES bounce notification.</summary>
        class AmazonSesBounceNotification
        {
            public string notificationType { get; set; }
            public AmazonSesBounce bounce { get; set; }
        }
        /// <summary>Represents meta data for the bounce notification from Amazon SES.</summary>
        class AmazonSesBounce
        {
            public string bounceType { get; set; }
            public string bounceSubType { get; set; }
            public DateTime timestamp { get; set; }
            public List<AmazonSesBouncedRecipient> bouncedRecipients { get; set; }
        }
        /// <summary>Represents the email address of recipients that bounced
        /// when sending from Amazon SES.</summary>
        class AmazonSesBouncedRecipient
        {
            public string emailAddress { get; set; }
        }
    }
}
