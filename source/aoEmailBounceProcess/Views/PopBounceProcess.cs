
using System;
using Contensive.BaseClasses;
using System.Linq;

namespace Contensive.Addons.EmailBounceProcess {
    public class PopProcessClass : AddonBaseClass {
        // 
        // ===========================================================================================
        /// <summary>
        /// if bounce configured, read all bounce email in and act on the sender
        /// </summary>
        /// <param name="CP"></param>
        /// <returns></returns>
        public override object Execute(CPBaseClass CP) {
            string returnHtml = "";
            try {
                bounceProcess(CP);
            } catch (Exception ex) {
                CP.Site.ErrorReport(ex);
            }
            return returnHtml;
        }
        // 
        // ===========================================================================================
        // 
        public void bounceProcess(Contensive.BaseClasses.CPBaseClass cp) {
            try {
                CPCSBaseClass CS = cp.CSNew();
                string MessageText;
                string[] FilterLines;
                string[] FilterText = Array.Empty<string>();
                int[] FilterType = Array.Empty<int>();
                int LinePtr;
                string[] LineSplit;
                int FilterLineCnt=0;
                string Filter;
                int BounceType;
                string EmailAddress;
                string PopServer;
                int popPort;
                string POPServerUsername;
                string POPServerPassword;
                int EmailBounceProcessAction;
                bool AllowEmailBounceProcessing;
                string bounceLogPathPage;
                string ActionTaken;
                string FilterFilename;
                string Filename = "";
                DateTime rightNowDate = DateTime.Now.Date;
                string logDatePart = rightNowDate.Year + rightNowDate.Month.ToString().PadLeft(2) + rightNowDate.Day.ToString().PadLeft(2);
                string amazonMsg = "An error occurred while trying to deliver the mail to the following recipients:" + "\r\n";
                // 
                AllowEmailBounceProcessing = cp.Site.GetBoolean("AllowEmailBounceProcessing", false);
                if (AllowEmailBounceProcessing) {
                    PopServer = cp.Site.GetText("PopServer", "");
                    popPort = cp.Site.GetInteger("popServerPort", 110);
                    if (popPort <= 0)
                        popPort = 110;
                    POPServerUsername = cp.Site.GetText("POPServerUsername", "");
                    POPServerPassword = cp.Site.GetText("POPServerPassword", "");
                    if ((PopServer == "") | (POPServerUsername == "") | (POPServerPassword == ""))
                        cp.Utils.AppendLog("AllowEmailBounceProcessing true but server, username or password is blank");
                    else {
                        bounceLogPathPage = @"BounceLog\" + logDatePart + @"\trace.txt";
                        FilterFilename = @"\config\EmailBounceFilters.txt";
                        EmailBounceProcessAction = cp.Site.GetInteger("EmailBounceProcessAction", 0);
                        // 
                        // Read in the filter file
                        // 
                        if (true) {
                            string copy;
                            copy = cp.CdnFiles.Read(FilterFilename);
                            if (copy == "")
                                cp.Utils.AppendLog(@"Bounce processing filters file \config\EmailBounceFilters.txt is empty");
                            else {
                                copy = copy.Replace("\r\n", "\r");
                                copy = copy.Replace( "\n", "\r");
                                FilterLines = copy.Split('\r');
                                FilterLineCnt = FilterLines.Length;
                                FilterText = new string[FilterLineCnt + 100 + 1];
                                FilterType = new int[FilterLineCnt + 100 + 1];
                                // 
                                // 
                                // 
                                for (LinePtr = 0; LinePtr <= FilterLineCnt - 1; LinePtr++) {
                                    if (FilterLines[LinePtr] != "") {
                                        LineSplit = FilterLines[LinePtr].Split( ',');
                                        FilterText[LinePtr] = LineSplit[0];
                                        if (LineSplit.Length > 0)
                                            FilterType[LinePtr] = cp.Utils.EncodeInteger(LineSplit[1]);
                                    }
                                }
                                // 
                                // add amazon
                                // 
                                FilterText[FilterLineCnt] = amazonMsg;
                                FilterType[FilterLineCnt] = 2;
                                FilterLineCnt += 1;
                            }
                        }
                        // 
                        // Retrieve the emails
                        // 
                        int MessageCnt;
                        string headerList;
                        OpenPop.Mime.Message msg;
                        using (OpenPop.Pop3.Pop3Client pop = new OpenPop.Pop3.Pop3Client()) {
                            try {
                                pop.Connect(PopServer, popPort, true);
                                pop.Authenticate(POPServerUsername, POPServerPassword);
                                MessageCnt = pop.GetMessageCount();
                                // 
                                cp.CdnFiles.Append(bounceLogPathPage, "\r\n" + "New bounce emails, cnt=" + MessageCnt);
                                // 
                                for (int msgPtr = 1; msgPtr <= MessageCnt; msgPtr++) {
                                    msg = pop.GetMessage(msgPtr);
                                    headerList = "";
                                    EmailAddress = "";
                                    headerList = "";
                                    MessageText = "";
                                    if (!msg.Headers.From.HasValidMailAddress)
                                        // 
                                        cp.CdnFiles.Append(bounceLogPathPage, "\n\r" + "email" + msgPtr + "-" + "email address not found");
                                    else {
                                        EmailAddress = msg.Headers.From.Address;

                                        foreach (string key in msg.Headers.UnknownHeaders.AllKeys) {
                                            string keyValue = msg.Headers.UnknownHeaders[key];
                                            headerList += "\r\n" + key + "=" + keyValue;
                                        }

                                        OpenPop.Mime.MessagePart msgBody;
                                        msgBody = msg.FindFirstPlainTextVersion();
                                        if ((msgBody == null))
                                            msgBody = msg.FindFirstHtmlVersion();
                                        MessageText = "";
                                        if (!(msgBody == null)) {
                                            MessageText = msgBody.GetBodyAsText();
                                        }

                                        if (string.IsNullOrEmpty(MessageText))
                                            // 
                                            cp.CdnFiles.Append(bounceLogPathPage, "\n\r" + "email" + msgPtr + "-" + "email has blank body");
                                        else {
                                            // 
                                            // Process them as they come in
                                            // 
                                            if ((EmailAddress == "MAILER-DAEMON@amazonses.com")) {
                                                if ((MessageText.IndexOf(amazonMsg) > -1))
                                                    EmailAddress = MessageText.Replace(amazonMsg, "");
                                            }
                                            ActionTaken = "no action";
                                            if (EmailAddress == "") {
                                                // 
                                                cp.CdnFiles.Append(bounceLogPathPage, "\n\r" + "email" + msgPtr + "-" + "email address was blank");
                                                // 
                                                ActionTaken = "deleted with no action, email address could not be determined, email content saved [" + Filename + "]";
                                            } else if (FilterLineCnt == 0) {
                                                // 
                                                cp.CdnFiles.Append(bounceLogPathPage, "\n\r" + "email" + msgPtr + "-" + "email filter file was not found (" + FilterFilename + ")");
                                                // 
                                                ActionTaken = "[" + EmailAddress + "], deleted with no action, no Filter File [" + FilterFilename + "]";
                                            } else
                                                // Copy = strDecodeMime(MessageText, MessageHeaders)
                                                for (LinePtr = 0; LinePtr <= FilterLineCnt - 1; LinePtr++) {
                                                    Filter = FilterText[LinePtr].Trim();
                                                    if (Filter != "") {
                                                        if (MessageText.IndexOf(Filter) >= 0) {
                                                            BounceType = FilterType[LinePtr];
                                                            switch (BounceType) {
                                                                case 0: {
                                                                        // 
                                                                        ActionTaken = "[" + EmailAddress + "], deleted with no action, Filter [" + Filter + "] is not a bounce";
                                                                        break;
                                                                    }

                                                                case 1: {
                                                                        // 
                                                                        // soft bounce - may recover
                                                                        // 
                                                                        ActionTaken = "[" + EmailAddress + "], deleted with no action, Filter [" + Filter + "] is soft error, may recover";
                                                                        break;
                                                                    }

                                                                case 2: {
                                                                        // 
                                                                        // hard bounce - take action on the member email
                                                                        // 
                                                                        // 
                                                                        // 
                                                                        // 
                                                                        // 
                                                                        EmailBounceProcessAction = 1;
                                                                        // 
                                                                        // 
                                                                        // 
                                                                        switch (EmailBounceProcessAction) {
                                                                            case 1: {
                                                                                    // 
                                                                                    // clear allowgroupemail
                                                                                    // 
                                                                                    ActionTaken = "[" + EmailAddress + "], clear allowBulkEmail action, Filter [" + Filter + "] is hard error";
                                                                                    CS.Open("people", "email=" + cp.Db.EncodeSQLText(EmailAddress), "", true, "ID,Name,OrganizationID,allowbulkemail");
                                                                                    if (!(CS.OK()))
                                                                                        ActionTaken += ", NO RECORD FOUND";
                                                                                    else {
                                                                                        ActionTaken += ", clearing allowGroupEmail for records [";
                                                                                        while (CS.OK()) {
                                                                                            ActionTaken += "," + CS.GetInteger("id").ToString();
                                                                                            CS.SetField("allowbulkemail", 0.ToString());
                                                                                            CS.GoNext();
                                                                                        }
                                                                                        ActionTaken += "]";
                                                                                    }
                                                                                    CS.Close();
                                                                                    break;
                                                                                }

                                                                            case 2: {
                                                                                    // 
                                                                                    // clear email
                                                                                    // 
                                                                                    ActionTaken = "[" + EmailAddress + "], clear email address action, Filter [" + Filter + "] is hard error";
                                                                                    CS.Open("people", "email=" + cp.Db.EncodeSQLText(EmailAddress), "", true, "ID,Name,OrganizationID,email");
                                                                                    if (!CS.OK())
                                                                                        ActionTaken += ", NO RECORD FOUND";
                                                                                    else {
                                                                                        ActionTaken += ", clear email address for records [";
                                                                                        while (CS.OK()) {
                                                                                            CS.SetField("email", "");
                                                                                            CS.GoNext();
                                                                                        }
                                                                                        ActionTaken += "]";
                                                                                    }
                                                                                    CS.Close();
                                                                                    break;
                                                                                }

                                                                            case 3: {
                                                                                    // 
                                                                                    // Delete Member
                                                                                    // 
                                                                                    ActionTaken = "[" + EmailAddress + "], delete member, Filter [" + Filter + "] is hard error";
                                                                                    CS.Open("people", "email=" + cp.Db.EncodeSQLText(EmailAddress), "", true, "ID,Name,OrganizationID");
                                                                                    if (!CS.OK())
                                                                                        ActionTaken += ", NO RECORD FOUND";
                                                                                    else {
                                                                                        ActionTaken += ", delete people records [";
                                                                                        while (CS.OK()) {
                                                                                            CS.Delete();
                                                                                            CS.GoNext();
                                                                                        }
                                                                                        ActionTaken += "]";
                                                                                    }
                                                                                    CS.Close();
                                                                                    break;
                                                                                }

                                                                            default: {
                                                                                    // 
                                                                                    // Unknown Process Action
                                                                                    // 
                                                                                    ActionTaken = "[" + EmailAddress + "], deleted with no action, Filter [" + Filter + "] is hard error, but Process Action is unknown [" + EmailBounceProcessAction + "]";
                                                                                    break;
                                                                                }
                                                                        }

                                                                        break;
                                                                    }
                                                            }
                                                            // 
                                                            cp.CdnFiles.Append(bounceLogPathPage, "\n\r" + "email" + msgPtr + "-" + ActionTaken);
                                                            // 
                                                            break;
                                                        }
                                                    }
                                                }
                                        }
                                    }
                                    // 
                                    // save bounced email
                                    // 
                                    cp.CdnFiles.Save(@"BounceLog\" + logDatePart + @"\email-" + msgPtr + ".txt", EmailAddress + "\n\r" + headerList + "\n\r" + MessageText);
                                    // 
                                    // delete the message
                                    // 
                                    pop.DeleteMessage(msgPtr);
                                }
                            } catch (Exception ex) {
                                cp.Site.ErrorReport(ex, "Bounce Processing exception");
                            } finally {
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                cp.Site.ErrorReport(ex);
            }
        }


        // Private Function strDecodeMime(ByVal strMessage As String, ByVal strHeaders As String) As String
        // On Error GoTo ErrorHandling
        // '
        // Dim i As Integer
        // '
        // MIME1.ResetData()
        // MIME1.Message = strMessage
        // MIME1.MessageHeaders = strHeaders
        // MIME1.DecodeFromString()

        // 'decode message parts to find text part and filename attachements
        // For i = 0 To MIME1.PartCount - 1

        // If InStr(1, UCase(MIME1.PartContentType(i)), "MULTIPART") <> 0 Then
        // strDecodeMime = "This is a Multipart message with embedded parts.  You should, " & _
        // "recursively decode this message with the mime object to view all the parts"
        // End If

        // 'If MIME1.PartFilename(i) <> "" Then
        // '    cboAttach.AddItem MIME1.PartFilename(i)
        // '    cboAttach.ItemData(cboAttach.NewIndex) = i
        // 'End If

        // If UCase(MIME1.PartContentType(i)) = "TEXT/PLAIN" And MIME1.PartFilename(i) = "" Then
        // strDecodeMime = MIME1.PartDecodedString(i)
        // End If

        // Next i
        // Exit Function

        // ErrorHandling:

        // If Err.Number = 20283 Then 'no mime boundary found
        // strDecodeMime = strMessage
        // End If

        // End Function

        // Private Sub POP1_PITrail(Direction As Integer, Message As String)

        // '    Debug.Print Message

        // End Sub
        // 
        // 
        // 
        private string getEmailAddress(CPBaseClass cp, string MessageText) {
            string result = "";
            int Pos;
            int EOL;
            // 
            Pos = MessageText.IndexOf("Final-Recipient:");
            if (Pos != 0) {
                EOL = MessageText.IndexOf("\n\r", Pos);
                if (EOL != 0) {
                    result = MessageText.Substring(Pos+16,EOL - (Pos + 16));
                    result = result.Replace("rfc822;", "");
                    result = result.Trim();
                }
            }
            return result;
        }
    }
}




