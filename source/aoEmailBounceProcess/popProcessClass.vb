
Option Explicit On
Option Strict On

Imports Contensive.BaseClasses
Imports Contensive.Addons

Namespace Contensive.Addons.aoEmailBounce
    Public Class popProcessClass
        Inherits AddonBaseClass
        '
        Dim strBuffer As String
        '
        '===========================================================================================
        ''' <summary>
        ''' if bounce configured, read all bounce email in and act on the sender
        ''' </summary>
        ''' <param name="cp"></param>
        ''' <remarks></remarks>
        Public Overrides Function Execute(ByVal CP As CPBaseClass) As Object
            Dim returnHtml As String = ""
            Try
                Call bounceProcess(CP)
            Catch ex As Exception
                CP.Site.ErrorReport(ex)
            End Try
            Return returnHtml
        End Function
        '
        '
        '
        Public Sub bounceProcess(cp As Contensive.BaseClasses.CPBaseClass)
            Try
                Dim popClient As Pop3.Pop3Client
                Dim CS As CPCSBaseClass = cp.CSNew()
                Dim Ptr As Integer
                'Dim Copy As String
                Dim MessageText As String
                Dim MessageHeaders As String
                Dim MessageUID As String
                Dim FilterLines() As String
                Dim FilterText() As String
                Dim FilterType() As Integer
                'Dim FS As New FileSystemClass
                Dim LinePtr As Integer
                Dim LineSplit() As String
                Dim FilterLineCnt As Integer
                Dim Filter As String
                Dim BounceType As Integer
                Dim SQL As String
                Dim EmailAddress As String
                Dim PopServer As String
                Dim popPort As Integer
                Dim POPServerUsername As String
                Dim POPServerPassword As String
                Dim EmailBounceProcessAction As Integer
                Dim AllowEmailBounceProcessing As Boolean
                Dim bounceLogPathPage As String
                Dim ActionTaken As String
                Dim FilterFilename As String
                Dim Filename As String
                Dim rightNowDate As Date = Now.Date
                Dim logDatePart As String = rightNowDate.Year & rightNowDate.Month.ToString.PadLeft(2) & rightNowDate.Day.ToString.PadLeft(2)
                Dim amazonMsg As String = "An error occurred while trying to deliver the mail to the following recipients:" & vbCrLf
                '
                AllowEmailBounceProcessing = cp.Site.GetBoolean("AllowEmailBounceProcessing", "0")
                If AllowEmailBounceProcessing Then
                    PopServer = cp.Site.GetText("PopServer", "")
                    popPort = cp.Site.GetInteger("popServerPort", "110")
                    If popPort <= 0 Then
                        popPort = 110
                    End If
                    POPServerUsername = cp.Site.GetText("POPServerUsername", "")
                    POPServerPassword = cp.Site.GetText("POPServerPassword", "")
                    If (PopServer = "") Or (POPServerUsername = "") Or (POPServerPassword = "") Then
                        Call cp.Utils.AppendLogFile("AllowEmailBounceProcessing true but server, username or password is blank")
                    Else
                        bounceLogPathPage = "BounceLog\" & logDatePart & "\trace.txt"
                        FilterFilename = cp.Site.PhysicalInstallPath & "\config\EmailBounceFilters.txt"
                        EmailBounceProcessAction = cp.Site.GetInteger("EmailBounceProcessAction", "0")
                        '
                        ' Read in the filter file
                        '
                        If True Then
                            Dim copy As String
                            copy = cp.File.Read(FilterFilename)
                            If copy = "" Then
                                Call cp.Utils.AppendLogFile("Bounce processing filters file \config\EmailBounceFilters.txt is empty")
                            Else
                                copy = Replace(copy, vbCrLf, vbLf)
                                copy = Replace(copy, vbCr, vbLf)
                                FilterLines = Split(copy, vbLf)
                                FilterLineCnt = UBound(FilterLines) + 1
                                ReDim FilterText(FilterLineCnt + 100)
                                ReDim FilterType(FilterLineCnt + 100)
                                '
                                '
                                '
                                For LinePtr = 0 To FilterLineCnt - 1
                                    If FilterLines(LinePtr) <> "" Then
                                        LineSplit = Split(FilterLines(LinePtr), ",")
                                        FilterText(LinePtr) = LineSplit(0)
                                        If UBound(LineSplit) > 0 Then
                                            FilterType(LinePtr) = cp.Utils.EncodeInteger(LineSplit(1))
                                        End If
                                    End If
                                Next
                                '
                                ' add amazon
                                '
                                FilterText(FilterLineCnt) = amazonMsg
                                FilterType(FilterLineCnt) = 2
                                FilterLineCnt += 1
                            End If
                        End If
                        '
                        ' Retrieve the emails
                        '
                        Dim tmp As String = ""
                        Dim MessageCnt As Integer
                        Dim headerList As String
                        Dim msg As OpenPop.Mime.Message
                        Using pop As New OpenPop.Pop3.Pop3Client()
                            Try
                                pop.Connect(PopServer, popPort, True)
                                pop.Authenticate(POPServerUsername, POPServerPassword)
                                MessageCnt = pop.GetMessageCount()
                                '
                                cp.File.AppendVirtual(bounceLogPathPage, vbCrLf & "New bounce emails, cnt=" & MessageCnt)
                                '
                                For msgPtr As Integer = 1 To MessageCnt
                                    msg = pop.GetMessage(msgPtr)
                                    headerList = ""
                                    EmailAddress = ""
                                    headerList = ""
                                    MessageText = ""
                                    If Not msg.Headers.From.HasValidMailAddress Then
                                        '
                                        cp.File.AppendVirtual(bounceLogPathPage, vbCrLf & "email" & msgPtr & "-" & "email address not found")
                                        '
                                    Else
                                        EmailAddress = msg.Headers.From.Address

                                        For Each key As String In msg.Headers.UnknownHeaders.AllKeys()
                                            Dim keyValue As String = msg.Headers.UnknownHeaders.Item(key)
                                            headerList &= vbCrLf & key & "=" & keyValue
                                        Next

                                        Dim msgBody As OpenPop.Mime.MessagePart
                                        msgBody = msg.FindFirstPlainTextVersion
                                        If (msgBody Is Nothing) Then
                                            msgBody = msg.FindFirstHtmlVersion
                                        End If
                                        MessageText = ""
                                        If Not (msgBody Is Nothing) Then
                                            MessageText = msgBody.GetBodyAsText()
                                            tmp = tmp
                                        End If

                                        If String.IsNullOrEmpty(MessageText) Then
                                            '
                                            cp.File.AppendVirtual(bounceLogPathPage, vbCrLf & "email" & msgPtr & "-" & "email has blank body")
                                            '
                                        Else
                                            '
                                            ' Process them as they come in
                                            '
                                            If (EmailAddress = "MAILER-DAEMON@amazonses.com") Then
                                                If (MessageText.IndexOf(amazonMsg) > -1) Then
                                                    EmailAddress = MessageText.Replace(amazonMsg, "")
                                                End If
                                                EmailAddress = EmailAddress
                                                '
                                            End If
                                            ActionTaken = "no action"
                                            MessageHeaders = "" ' POP1.MessageHeaders
                                            If EmailAddress = "" Then
                                                '
                                                cp.File.AppendVirtual(bounceLogPathPage, vbCrLf & "email" & msgPtr & "-" & "email address was blank")
                                                '
                                                ActionTaken = "deleted with no action, email address could not be determined, email content saved [" & Filename & "]"
                                            Else
                                                If FilterLineCnt = 0 Then
                                                    '
                                                    cp.File.AppendVirtual(bounceLogPathPage, vbCrLf & "email" & msgPtr & "-" & "email filter file was not found (" & FilterFilename & ")")
                                                    '
                                                    ActionTaken = "[" & EmailAddress & "], deleted with no action, no Filter File [" & FilterFilename & "]"
                                                Else
                                                    'Copy = strDecodeMime(MessageText, MessageHeaders)
                                                    For LinePtr = 0 To FilterLineCnt - 1
                                                        Filter = Trim(FilterText(LinePtr))
                                                        If Filter <> "" Then
                                                            If InStr(1, MessageText, Filter, vbTextCompare) <> 0 Then
                                                                BounceType = FilterType(LinePtr)
                                                                Select Case BounceType
                                                                    Case 0
                                                                        '
                                                                        ActionTaken = "[" & EmailAddress & "], deleted with no action, Filter [" & Filter & "] is not a bounce"
                                                                        ''Call AppendEmailLog(Csv.ApplicationNameLocal, "Process", ActionTaken)
                                                                    Case 1
                                                                        '
                                                                        ' soft bounce - may recover
                                                                        '
                                                                        ActionTaken = "[" & EmailAddress & "], deleted with no action, Filter [" & Filter & "] is soft error, may recover"
                                                                        ''Call AppendEmailLog(Csv.ApplicationNameLocal, "Process", ActionTaken)
                                                                    Case 2
                                                                        '
                                                                        ' hard bounce - take action on the member email
                                                                        '
                                                                        '
                                                                        '
                                                                        '
                                                                        '
                                                                        EmailBounceProcessAction = 1
                                                                        '
                                                                        '
                                                                        '
                                                                        Select Case EmailBounceProcessAction
                                                                            Case 1
                                                                                '
                                                                                ' clear allowgroupemail
                                                                                '
                                                                                ActionTaken = "[" & EmailAddress & "], clear allowBulkEmail action, Filter [" & Filter & "] is hard error"
                                                                                Call CS.Open("people", "email=" & cp.Db.EncodeSQLText(EmailAddress), , , "ID,Name,OrganizationID,allowbulkemail")
                                                                                If Not (CS.OK) Then
                                                                                    ActionTaken &= ", NO RECORD FOUND"
                                                                                Else
                                                                                    ActionTaken &= ", clearing allowGroupEmail for records ["
                                                                                    Do While CS.OK()
                                                                                        ActionTaken &= "," & CS.GetInteger("id").ToString
                                                                                        Call CS.SetField("allowbulkemail", 0.ToString())
                                                                                        Call CS.GoNext()
                                                                                    Loop
                                                                                    ActionTaken &= "]"
                                                                                End If
                                                                                Call CS.Close()
                                                                            Case 2
                                                                                '
                                                                                ' clear email
                                                                                '
                                                                                ActionTaken = "[" & EmailAddress & "], clear email address action, Filter [" & Filter & "] is hard error"
                                                                                Call CS.Open("people", "email=" & cp.Db.EncodeSQLText(EmailAddress), , , "ID,Name,OrganizationID,email")
                                                                                If Not CS.OK Then
                                                                                    ActionTaken &= ", NO RECORD FOUND"
                                                                                Else
                                                                                    ActionTaken &= ", clear email address for records ["
                                                                                    Do While CS.OK()
                                                                                        Call CS.SetField("email", "")
                                                                                        Call CS.GoNext()
                                                                                    Loop
                                                                                    ActionTaken &= "]"
                                                                                End If
                                                                                Call CS.Close()
                                                                            Case 3
                                                                                '
                                                                                ' Delete Member
                                                                                '
                                                                                ActionTaken = "[" & EmailAddress & "], delete member, Filter [" & Filter & "] is hard error"
                                                                                Call CS.Open("people", "email=" & cp.Db.EncodeSQLText(EmailAddress), , , "ID,Name,OrganizationID")
                                                                                If Not CS.OK Then
                                                                                    ActionTaken &= ", NO RECORD FOUND"
                                                                                Else
                                                                                    ActionTaken &= ", delete people records ["
                                                                                    Do While CS.OK()
                                                                                        Call CS.Delete()
                                                                                        Call CS.GoNext()
                                                                                    Loop
                                                                                    ActionTaken &= "]"
                                                                                End If
                                                                                Call CS.Close()
                                                                            Case Else
                                                                                '
                                                                                ' Unknown Process Action
                                                                                '
                                                                                ActionTaken = "[" & EmailAddress & "], deleted with no action, Filter [" & Filter & "] is hard error, but Process Action is unknown [" & EmailBounceProcessAction & "]"
                                                                                ''Call AppendEmailLog(Csv.ApplicationNameLocal, "Process", ActionTaken)
                                                                        End Select
                                                                End Select
                                                                '
                                                                cp.File.AppendVirtual(bounceLogPathPage, vbCrLf & "email" & msgPtr & "-" & ActionTaken)
                                                                '
                                                                Exit For
                                                            End If
                                                        End If
                                                    Next
                                                End If
                                            End If

                                        End If
                                    End If
                                    '
                                    ' save bounced email
                                    '
                                    Call cp.File.SaveVirtual("BounceLog\" & logDatePart & "\email-" & msgPtr & ".txt", EmailAddress & vbCrLf & headerList & vbCrLf & MessageText)
                                    '
                                    ' delete the message
                                    '
                                    pop.DeleteMessage(msgPtr)
                                Next

                            Catch ex As Exception
                                cp.Site.ErrorReport(ex, "Bounce Processing exception")
                            Finally
                            End Try
                        End Using
                    End If
                End If
            Catch ex As Exception
                cp.Site.ErrorReport(ex)
            End Try
        End Sub


        '        Private Function strDecodeMime(ByVal strMessage As String, ByVal strHeaders As String) As String
        '            On Error GoTo ErrorHandling
        '            '
        '            Dim i As Integer
        '            '
        '            MIME1.ResetData()
        '            MIME1.Message = strMessage
        '            MIME1.MessageHeaders = strHeaders
        '            MIME1.DecodeFromString()

        '            'decode message parts to find text part and filename attachements
        '            For i = 0 To MIME1.PartCount - 1

        '                If InStr(1, UCase(MIME1.PartContentType(i)), "MULTIPART") <> 0 Then
        '                    strDecodeMime = "This is a Multipart message with embedded parts.  You should, " & _
        '                        "recursively decode this message with the mime object to view all the parts"
        '                End If

        '                'If MIME1.PartFilename(i) <> "" Then
        '                '    cboAttach.AddItem MIME1.PartFilename(i)
        '                '    cboAttach.ItemData(cboAttach.NewIndex) = i
        '                'End If

        '                If UCase(MIME1.PartContentType(i)) = "TEXT/PLAIN" And MIME1.PartFilename(i) = "" Then
        '                    strDecodeMime = MIME1.PartDecodedString(i)
        '                End If

        '            Next i
        '            Exit Function

        'ErrorHandling:

        '            If Err.Number = 20283 Then 'no mime boundary found
        '                strDecodeMime = strMessage
        '            End If

        '        End Function

        '        Private Sub POP1_PITrail(Direction As Integer, Message As String)

        '            '    Debug.Print Message

        '        End Sub
        '
        '
        '
        Private Function GetEmailAddress(cp As CPBaseClass, MessageText As String) As String
            GetEmailAddress = ""
            Dim Pos As Integer
            Dim EOL As Integer
            '
            Pos = InStr(1, MessageText, "Final-Recipient:", vbTextCompare)
            If Pos <> 0 Then
                EOL = InStr(Pos, MessageText, vbCrLf)
                If EOL <> 0 Then
                    GetEmailAddress = Mid(MessageText, Pos + 16, EOL - (Pos + 16))
                    GetEmailAddress = Replace(GetEmailAddress, "rfc822;", "", 1, 99, vbTextCompare)
                    GetEmailAddress = Trim(GetEmailAddress)
                End If
            End If
        End Function
    End Class
End Namespace