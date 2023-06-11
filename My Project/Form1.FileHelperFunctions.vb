Option Strict On

Partial Class Form1

    Private Sub UpdateListViewFiles(Source As ListViewItem, BareTopLevelAssembly As Boolean)

        Dim FoundFiles As IReadOnlyCollection(Of String) = Nothing
        Dim FoundFile As String
        Dim ActiveFileExtensionsList As New List(Of String)
        Dim tf As Boolean
        'Dim msg As String

        Dim StartupPath As String = System.Windows.Forms.Application.StartupPath()
        Dim TODOFile As String = String.Format("{0}\{1}", StartupPath, "todo.txt")

        Dim ElapsedTime As Double
        Dim ElapsedTimeText As String

        StartTime = Now

        ListViewFilesOutOfDate = False
        BT_Update.BackColor = Color.FromName("Control")

        StopProcess = False
        ButtonCancel.Text = "Stop"

        If new_CheckBoxFilterAsm.Checked Then
            ActiveFileExtensionsList.Add("*.asm")
        End If
        If new_CheckBoxFilterPar.Checked Then
            ActiveFileExtensionsList.Add("*.par")
        End If
        If new_CheckBoxFilterPsm.Checked Then
            ActiveFileExtensionsList.Add("*.psm")
        End If
        If new_CheckBoxFilterDft.Checked Then
            ActiveFileExtensionsList.Add("*.dft")
        End If

        If ActiveFileExtensionsList.Count > 0 Then

            Select Case Source.Tag.ToString
                Case = "Folder"
                    If FileIO.FileSystem.DirectoryExists(Source.Name) Then
                        FoundFiles = FileIO.FileSystem.GetFiles(Source.Name,
                                     FileIO.SearchOption.SearchTopLevelOnly,
                                     ActiveFileExtensionsList.ToArray)
                    End If

                Case = "Folders"
                    If FileIO.FileSystem.DirectoryExists(Source.Name) Then FoundFiles = FileIO.FileSystem.GetFiles(Source.Name,
                                    FileIO.SearchOption.SearchAllSubDirectories,
                                    ActiveFileExtensionsList.ToArray)

                Case = "csv", "txt"
                    If FileIO.FileSystem.FileExists(Source.Name) Then FoundFiles = IO.File.ReadAllLines(Source.Name)

                Case = "excel"
                    If FileIO.FileSystem.FileExists(Source.Name) Then FoundFiles = CommonTasks.ReadExcel(Source.Name)

                Case = "ASM_Folder"
                    ' Nothing to do here.  Dealt with in 'Case = "asm"'

                Case = "asm"

                    If (Not BareTopLevelAssembly) And (FileIO.FileSystem.FileExists(Source.Name)) Then
                        Dim tmpFolders As New List(Of String)

                        For Each item As ListViewItem In ListViewFiles.Items
                            If item.Tag.ToString = "ASM_Folder" Then
                                If Not tmpFolders.Contains(item.Name) Then tmpFolders.Add(item.Name)
                            End If
                        Next

                        Dim tmpFoundFiles As New List(Of String)

                        If RadioButtonTLABottomUp.Checked Then
                            Dim TLAU As New TopLevelAssemblyUtilities(Me)

                            TextBoxStatus.Text = "Finding all linked files.  This may take some time."

                            tmpFoundFiles.AddRange(TLAU.GetLinksBottomUp(tmpFolders,
                                                           Source.SubItems.Item(1).Text,
                                                           ActiveFileExtensionsList,
                                                           CheckBoxDraftAndModelSameName.Checked,
                                                           CheckBoxTLAReportUnrelatedFiles.Checked))

                            'For Each tmpFolder As String In tmpFolders

                            '    'Dim TLAU As New TopLevelAssemblyUtilities(Me)

                            '    'TextBoxStatus.Text = "Finding all linked files.  This may take some time."

                            '    'tmpFoundFiles.AddRange(TLAU.GetLinksBottomUp(tmpFolder,
                            '    '                           Source.SubItems.Item(1).Text,
                            '    '                           ActiveFileExtensionsList,
                            '    '                           CheckBoxDraftAndModelSameName.Checked))
                            'Next
                        Else
                            Dim TLAU As New TopLevelAssemblyUtilities(Me)
                            tmpFoundFiles.AddRange(TLAU.GetLinksTopDown(tmpFolders,
                                                       Source.SubItems.Item(1).Text,
                                                       ActiveFileExtensionsList,
                                                       Report:=CheckBoxTLAReportUnrelatedFiles.Checked))
                        End If

                        FoundFiles = CType(tmpFoundFiles, IReadOnlyCollection(Of String))

                        TextBoxStatus.Text = ""

                    ElseIf (BareTopLevelAssembly) And (FileIO.FileSystem.FileExists(Source.Name)) Then

                        Dim TLAU As New TopLevelAssemblyUtilities(Me)

                        TextBoxStatus.Text = "Finding all linked files.  This may take some time."

                        Dim tmpFoundFiles As New List(Of String)
                        Dim tmpFolders As New List(Of String)

                        ' Empty list of strings is flag for BareTopLevelAssembly
                        ' No 'where used' is performed.
                        ' Bare top level assemblies are always processed bottom up.
                        tmpFoundFiles.AddRange(TLAU.GetLinksBottomUp(tmpFolders,
                                                           Source.SubItems.Item(1).Text,
                                                           ActiveFileExtensionsList,
                                                           CheckBoxDraftAndModelSameName.Checked,
                                                           CheckBoxTLAReportUnrelatedFiles.Checked))

                        FoundFiles = CType(tmpFoundFiles, IReadOnlyCollection(Of String))

                        TextBoxStatus.Text = ""

                    End If

                Case Else
                    FoundFiles = Nothing
                    TextBoxStatus.Text = "No files found"
            End Select


        End If

        ' Remove problem files
        If Not FoundFiles Is Nothing Then
            Dim tmpFoundFiles As New List(Of String)
            For Each item In FoundFiles
                tf = CommonTasks.FilenameIsOK(item)
                tf = tf And IO.File.Exists(item)
                tf = tf And Not tmpFoundFiles.Contains(item)
                ' Exporting from LibreOffice Calc to Excel, the first item can sometimes be Nothing
                ' Causes a problem comparing extensions
                Try
                    tf = tf And ActiveFileExtensionsList.Contains(IO.Path.GetExtension(item).Replace(".", "*."))
                Catch ex As Exception
                    ' MsgBox("Catch")
                End Try
                If tf Then
                    tmpFoundFiles.Add(item)
                End If
            Next
            FoundFiles = CType(tmpFoundFiles, IReadOnlyCollection(Of String))
        End If


        ' Dependency sort
        If Not FoundFiles Is Nothing Then
            If RadioButtonListSortDependency.Checked Then
                FoundFiles = GetDependencySortedFiles(FoundFiles)
            End If
        End If


        ' Run filters
        If Not FoundFiles Is Nothing Then

            ' Filter by file wildcard search
            If new_CheckBoxFileSearch.Checked Then
                FoundFiles = FileWildcardSearch(FoundFiles, new_ComboBoxFileSearch.Text)
            End If

            ' Filter by properties
            If new_CheckBoxEnablePropertyFilter.Checked Then
                System.Threading.Thread.Sleep(1000)
                Dim PropertyFilter As New PropertyFilter(Me)
                FoundFiles = PropertyFilter.PropertyFilter(FoundFiles, PropertyFilterDict, PropertyFilterFormula)
            End If

            If RadioButtonListSortAlphabetical.Checked Then
                FoundFiles = SortAlphabetical(FoundFiles)
            End If

            ListViewFiles.BeginUpdate()

            For Each FoundFile In FoundFiles

                TextBoxStatus.Text = String.Format("Updating List {0}", System.IO.Path.GetFileName(FoundFile))
                System.Windows.Forms.Application.DoEvents()

                If CommonTasks.FilenameIsOK(FoundFile) Then

                    If IO.File.Exists(FoundFile) Then

                        If Not ListViewFiles.Items.ContainsKey(FoundFile) Then

                            Dim tmpLVItem As New ListViewItem
                            tmpLVItem.Text = IO.Path.GetFileName(FoundFile)
                            tmpLVItem.SubItems.Add(IO.Path.GetDirectoryName(FoundFile))
                            tmpLVItem.ImageKey = "Unchecked"
                            tmpLVItem.Tag = IO.Path.GetExtension(FoundFile).ToLower 'Backup gruppo
                            tmpLVItem.Name = FoundFile
                            tmpLVItem.Group = ListViewFiles.Groups.Item(IO.Path.GetExtension(FoundFile).ToLower)
                            ListViewFiles.Items.Add(tmpLVItem)

                        End If

                    End If

                End If

            Next

            'ListViewFiles.AutoResizeColumn(0, ColumnHeaderAutoResizeStyle.ColumnContent)
            ListViewFiles.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.ColumnContent)

            ListViewFiles.EndUpdate()

            TextBoxStatus.Text = ""


            ElapsedTime = Now.Subtract(StartTime).TotalMinutes
            If ElapsedTime < 60 Then
                ElapsedTimeText = "in " + ElapsedTime.ToString("0.0") + " min."
            Else
                ElapsedTimeText = "in " + (ElapsedTime / 60).ToString("0.0") + " hr."
            End If


            'TextBoxStatus.Text = String.Format("{0} files found", FoundFiles.Count)
            TextBoxStatus.Text = String.Format("{0} files found in {1}", ListViewFiles.Items.Count, ElapsedTimeText)

        Else
            TextBoxStatus.Text = "No files found"
        End If


        StopProcess = False
        ButtonCancel.Text = "Cancel"

    End Sub

    Private Function FileWildcardSearch(
        ByVal FoundFiles As IReadOnlyCollection(Of String),
        ByVal WildcardString As String) As IReadOnlyCollection(Of String)

        Dim LocalFoundFiles As New List(Of String)
        Dim FilePath As String
        Dim Filename As String

        For Each FilePath In FoundFiles
            Filename = System.IO.Path.GetFileName(FilePath)
            TextBoxStatus.Text = String.Format("Wildcard Search {0}", Filename)
            If Filename Like WildcardString Then
                LocalFoundFiles.Add(FilePath)
            End If
        Next

        Return LocalFoundFiles
    End Function

    Private Function GetFileNames(ByVal FileWildcard As String) As List(Of String)
        ' Build up list of files to process depending on which option was selected.
        ' Dim FoundFiles As ObjectModel.ReadOnlyCollection(Of String)
        Dim FoundFilesArray As String() = {}
        Dim FoundFilesList As New List(Of String)
        Dim FileExtension As String = FileWildcard.Replace("*", "")
        Dim Filename As String

        TextBoxStatus.Text = "Getting files..."

        If ListViewFiles.SelectedItems.Count > 0 Then
            For i As Integer = 0 To ListViewFiles.SelectedItems.Count - 1
                Filename = ListViewFiles.SelectedItems.Item(i).Name
                If System.IO.Path.GetExtension(Filename) = FileExtension Then
                    FoundFilesList.Add(Filename)
                End If
            Next
        Else
            For i As Integer = 0 To ListViewFiles.Items.Count - 1
                Filename = ListViewFiles.Items(i).Name
                If System.IO.Path.GetExtension(Filename) = FileExtension Then
                    If ListViewFiles.Items(i).Group.Name <> "Excluded" Then FoundFilesList.Add(Filename)
                End If
            Next

        End If

        Return FoundFilesList
    End Function

    Private Function GetTotalFilesToProcess() As Integer
        Dim Count As Integer = 0

        If CheckedListBoxAssembly.CheckedItems.Count > 0 Then
            Count += GetFileNames("*.asm").Count
        End If
        If CheckedListBoxPart.CheckedItems.Count > 0 Then
            Count += GetFileNames("*.par").Count
        End If
        If CheckedListBoxSheetmetal.CheckedItems.Count > 0 Then
            Count += GetFileNames("*.psm").Count
        End If
        If CheckedListBoxDraft.CheckedItems.Count > 0 Then
            Count += GetFileNames("*.dft").Count
        End If

        Return Count
    End Function

    Private Function GetDependencySortedFiles(Foundfiles As IReadOnlyCollection(Of String)) As IReadOnlyCollection(Of String)
        Dim OutList As New List(Of String)
        Dim DependencyDict As New Dictionary(Of String, List(Of String))
        Dim DMApp As New DesignManager.Application
        Dim Filename As String
        Dim DMDoc As DesignManager.Document

        For Each Filename In Foundfiles

            TextBoxStatus.Text = String.Format("Opening {0}.  This can take some time.", IO.Path.GetFileName(Filename))

            DMDoc = CType(DMApp.Open(Filename), DesignManager.Document)

            Dim tmp_DependencyDict As New Dictionary(Of String, List(Of String))

            tmp_DependencyDict = GetLinks(DMDoc, tmp_DependencyDict)

            For Each s As String In tmp_DependencyDict.Keys
                TextBoxStatus.Text = s
                If Not DependencyDict.Keys.Contains(s) Then
                    DependencyDict.Add(s, tmp_DependencyDict(s))

                End If
            Next

        Next

        OutList = SortByDependency(DependencyDict)

        DMApp.Quit()

        Return OutList

    End Function

    Private Function GetLinks(
        DMDoc As DesignManager.Document,
        LinkDict As Dictionary(Of String, List(Of String))
        ) As Dictionary(Of String, List(Of String))

        'Dim LinkDict As New Dictionary(Of String, List(Of String))
        Dim LinkDoc As DesignManager.Document
        Dim LinkDocs As DesignManager.LinkedDocuments
        Dim DMDocName As String
        Dim LinkDocName As String
        'Dim Filename As String

        Dim tf As Boolean

        'For Each Filename In OriginalLinkDict.Keys
        '    LinkDict.Add(Filename, New List(Of String))
        'Next

        DMDocName = DMDoc.FullName
        If DMDocName.Contains("!") Then
            DMDocName = DMDocName.Split("!"c)(0)
        End If

        If Not LinkDict.Keys.Contains(DMDocName) Then

            LinkDict.Add(DMDocName, New List(Of String))

            ' Master FOP reports Child documents as LinkedDocuments.
            ' Not correct in this dependency context.
            'FOPStatus
            '1 Not FOP 
            '2 FOP Master
            '4 FOP not Master
            Dim FOPStatus As Integer
            DMDoc.IsDocumentFOP(FOPStatus)

            'tf = CommonTasks.GetDocTypeByExtension(DMDocName) = ".dft"
            'tf = tf Or (FOPStatus = 2)
            tf = FOPStatus = 2

            If Not tf Then
                LinkDocs = CType(DMDoc.LinkedDocuments, DesignManager.LinkedDocuments)
                If Not LinkDocs Is Nothing Then
                    If LinkDocs.Count > 0 Then
                        For Each LinkDoc In LinkDocs

                            LinkDocName = LinkDoc.FullName
                            If LinkDocName.Contains("!") Then
                                LinkDocName = LinkDocName.Split("!"c)(0)
                            End If

                            LinkDict(DMDocName).Add(LinkDocName)
                            LinkDict = GetLinks(LinkDoc, LinkDict)
                        Next
                    End If

                End If
            End If
        End If

        Return LinkDict
    End Function

    Private Function SortByDependency(
        OriginalLinkDict As Dictionary(Of String, List(Of String))
        ) As List(Of String)

        Dim LinkDict As New Dictionary(Of String, List(Of String))
        Dim Outlist As New List(Of String)
        Dim HasUnmetDependencies As Boolean
        Dim NoRemainingLinks As Boolean
        Dim NoDependencies As New List(Of String)
        Dim Filename As String

        Dim Count As Integer = 0
        Dim OldCount As Integer = 100

        ' Copy the original dictionary
        For Each Filename In OriginalLinkDict.Keys
            LinkDict.Add(Filename, OriginalLinkDict(Filename))
        Next

        ' Find files with no dependencies.
        For Each Filename In LinkDict.Keys
            If LinkDict(Filename).Count = 0 Then
                Outlist.Add(Filename)
                NoDependencies.Add(Filename)
            End If
        Next

        ' Remove files with no dependencies from the copied dictionary
        For Each Filename In Outlist
            If LinkDict.Keys.Contains(Filename) Then
                LinkDict.Remove(Filename)
            End If
        Next

        While OldCount > Count
            For Each Filename In LinkDict.Keys
                TextBoxStatus.Text = IO.Path.GetFileName(Filename)
                If LinkDict(Filename).Count = 0 Then
                    Outlist.Add(Filename)
                    Continue For
                Else
                    HasUnmetDependencies = False
                    For Each LinkFilename As String In LinkDict(Filename)
                        If Not Outlist.Contains(LinkFilename) Then
                            HasUnmetDependencies = True
                        End If
                    Next
                    If Not HasUnmetDependencies Then
                        Outlist.Add(Filename)
                    End If
                End If
            Next

            OldCount = LinkDict.Count

            For Each Filename In Outlist
                If LinkDict.Keys.Contains(Filename) Then
                    LinkDict.Remove(Filename)
                End If
            Next

            Count = LinkDict.Count

            ' Try to deal with mutual dependencies, like interpart copies
            ' Remove *.par first, then *.psm, *.asm, *.dft in order
            If OldCount = Count Then
                Dim ExtensionList As New List(Of String)
                Dim Extension As String
                Dim RemainingFileList As New List(Of String)

                For Each Filename In LinkDict.Keys
                    RemainingFileList.Add(Filename)
                    Extension = IO.Path.GetExtension(Filename)
                    If Not ExtensionList.Contains(Extension) Then
                        ExtensionList.Add(Extension)
                    End If
                Next

                If ExtensionList.Contains(".par") Then
                    For Each Filename In RemainingFileList
                        If IO.Path.GetExtension(Filename) = ".par" Then
                            Outlist.Add(Filename)
                            LinkDict.Remove(Filename)
                        End If
                    Next
                ElseIf ExtensionList.Contains(".psm") Then
                    For Each Filename In RemainingFileList
                        If IO.Path.GetExtension(Filename) = ".psm" Then
                            Outlist.Add(Filename)
                            LinkDict.Remove(Filename)
                        End If
                    Next
                ElseIf ExtensionList.Contains(".asm") Then
                    For Each Filename In RemainingFileList
                        If IO.Path.GetExtension(Filename) = ".asm" Then
                            Outlist.Add(Filename)
                            LinkDict.Remove(Filename)
                        End If
                    Next
                ElseIf ExtensionList.Contains(".dft") Then
                    For Each Filename In RemainingFileList
                        If IO.Path.GetExtension(Filename) = ".dft" Then
                            Outlist.Add(Filename)
                            LinkDict.Remove(Filename)
                        End If
                    Next
                End If

            End If

            Count = LinkDict.Count

        End While

        If Not CheckBoxListIncludeNoDependencies.Checked Then
            For Each Filename In NoDependencies
                If Outlist.Contains(Filename) Then
                    Outlist.Remove(Filename)
                End If
            Next

            Count = 0
            OldCount = 100
            While OldCount > Count
                OldCount = Outlist.Count
                For Each Filename In OriginalLinkDict.Keys
                    If IO.Path.GetExtension(Filename) = ".asm" Then
                        NoRemainingLinks = True
                        For Each LinkFilename As String In OriginalLinkDict(Filename)
                            If Outlist.Contains(LinkFilename) Then
                                NoRemainingLinks = False
                            End If
                        Next
                        If NoRemainingLinks Then
                            Outlist.Remove(Filename)
                        End If
                    End If
                Next
                Count = Outlist.Count
            End While

            For Each Filename In OriginalLinkDict.Keys
                If IO.Path.GetExtension(Filename) = ".dft" Then
                    NoRemainingLinks = True
                    For Each LinkFilename As String In OriginalLinkDict(Filename)
                        If Outlist.Contains(LinkFilename) Then
                            NoRemainingLinks = False
                        End If
                    Next
                    If NoRemainingLinks Then
                        Outlist.Remove(Filename)
                    End If
                End If
            Next

        End If



        'Dim msg As String = ""
        'If Count > 0 Then
        '    For Each Filename In LinkDict.Keys
        '        If Not Outlist.Contains(Filename) Then
        '            For Each LinkDocName As String In LinkDict(Filename)
        '                If Not Outlist.Contains(LinkDocName) Then
        '                    msg = String.Format("{0}{1}{1}{2}: {3}", msg, vbCrLf, IO.Path.GetFileName(Filename), LinkDocName)

        '                End If
        '            Next
        '        End If
        '    Next
        '    MsgBox(msg)
        'End If

        If Count > 0 Then
            For Each Filename In LinkDict.Keys
                Outlist.Add(Filename)
            Next
        End If

        Return Outlist

    End Function

    Private Function SortAlphabetical(FoundFiles As IReadOnlyCollection(Of String)) As IReadOnlyCollection(Of String)
        Dim Outlist As New List(Of String)
        Dim SortedOutlist As New List(Of String)
        Dim FoundFile As String
        Dim Filename As String
        Dim s As String

        For Each FoundFile In FoundFiles
            Filename = IO.Path.GetFileName(FoundFile)
            s = String.Format("{0}*{1}", Filename, FoundFile)
            Outlist.Add(s)
        Next

        Outlist.Sort()

        For Each s In Outlist
            Filename = s.Split(CType("*", Char()))(1)
            SortedOutlist.Add(Filename)
        Next

        Return SortedOutlist

    End Function

End Class