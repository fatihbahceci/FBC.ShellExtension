using System.Globalization;
using System.Resources;

namespace FBC.ShellExtension.Core.Resources;

/// <summary>
/// The single access point for user-visible text. Lookup falls back from the UI culture to
/// the neutral (English) resources and finally to the key itself, so a missing translation
/// shows up as a visible key instead of an empty label.
/// </summary>
public static class Strings
{
    private static readonly ResourceManager s_resources =
        new("FBC.ShellExtension.Core.Resources.Strings", typeof(Strings).Assembly);

    public static string Get(string key) =>
        s_resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    private static string Format(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), args);

    public static string ConfigEmpty() => Get(nameof(ConfigEmpty));

    public static string ConfigInvalidJson(string detail) => Format(nameof(ConfigInvalidJson), detail);

    public static string ValidationUnsupportedVersion(int found, int supported) =>
        Format(nameof(ValidationUnsupportedVersion), found, supported);

    public static string ValidationNullEntry() => Get(nameof(ValidationNullEntry));

    public static string ValidationLabelRequired() => Get(nameof(ValidationLabelRequired));

    public static string ValidationSubmenuEmpty() => Get(nameof(ValidationSubmenuEmpty));

    public static string ValidationCommandRequired() => Get(nameof(ValidationCommandRequired));

    public static string ValidationToolAndCommand() => Get(nameof(ValidationToolAndCommand));

    public static string ValidationToolNeedsVersion(int version) => Format(nameof(ValidationToolNeedsVersion), version);

    public static string ValidationEnabledNeedsVersion() => Get(nameof(ValidationEnabledNeedsVersion));

    public static string ValidationFileNameRequired() => Get(nameof(ValidationFileNameRequired));

    public static string ValidationMinCount() => Get(nameof(ValidationMinCount));

    public static string ValidationMaxCount(int minCount) => Format(nameof(ValidationMaxCount), minCount);

    public static string ValidationExtensionInvalid(string value) => Format(nameof(ValidationExtensionInvalid), value);

    public static string ValidationPatternInvalid(string value) => Format(nameof(ValidationPatternInvalid), value);

    public static string ValidationMaxParallelism() => Get(nameof(ValidationMaxParallelism));

    public static string ValidationVerbNeedsShellExecute(string verb) =>
        Format(nameof(ValidationVerbNeedsShellExecute), verb);

    public static string MenuNoConfig() => Get(nameof(MenuNoConfig));

    public static string MenuConfigInvalid() => Get(nameof(MenuConfigInvalid));

    public static string MenuEditConfig() => Get(nameof(MenuEditConfig));

    public static string JobInvalid(string detail) => Format(nameof(JobInvalid), detail);

    public static string JobUnsupportedVersion(int found, int supported) =>
        Format(nameof(JobUnsupportedVersion), found, supported);

    public static string RunnerUsage() => Get(nameof(RunnerUsage));

    public static string ValidationShowOutputNeedsNoShellExecute() => Get(nameof(ValidationShowOutputNeedsNoShellExecute));

    public static string ValidationShowOutputNeedsTrack() => Get(nameof(ValidationShowOutputNeedsTrack));

    public static string ExecCommandLineTooLong(int length, int max) => Format(nameof(ExecCommandLineTooLong), length, max);

    public static string ExecExitCode(string exitCode) => Format(nameof(ExecExitCode), exitCode);

    public static string ExecCancelled() => Get(nameof(ExecCancelled));

    public static string ExecNotStarted() => Get(nameof(ExecNotStarted));

    public static string ExecUnexpected(string detail) => Format(nameof(ExecUnexpected), detail);

    public static string ExecStartFailed(string fileName, string detail) => Format(nameof(ExecStartFailed), fileName, detail);

    public static string ExecElevationRefused() => Get(nameof(ExecElevationRefused));

    public static string ExecWorkerFailed(string detail) => Format(nameof(ExecWorkerFailed), detail);

    public static string WorkerProtocolError(string detail) => Format(nameof(WorkerProtocolError), detail);

    public static string SummaryHeadline(int succeeded, int failed) => Format(nameof(SummaryHeadline), succeeded, failed);

    public static string SummaryCancelled(int cancelled) => Format(nameof(SummaryCancelled), cancelled);

    public static string SummaryNotStarted(int notStarted) => Format(nameof(SummaryNotStarted), notStarted);

    public static string LogNotStartedReason(string subject, string reason) => Format(nameof(LogNotStartedReason), subject, reason);

    public static string ExecCommandLineTooLongPerFile(int length, int max) => Format(nameof(ExecCommandLineTooLongPerFile), length, max);

    public static string ExecCancelledIncomplete() => Get(nameof(ExecCancelledIncomplete));

    public static string RunnerLogTrimmed() => Get(nameof(RunnerLogTrimmed));

    public static string SummaryItems(int count) => Format(nameof(SummaryItems), count);

    public static string LogSucceeded(string subject) => Format(nameof(LogSucceeded), subject);

    public static string LogFailed(string subject, string message) => Format(nameof(LogFailed), subject, message);

    public static string LogCancelled(string subject) => Format(nameof(LogCancelled), subject);

    public static string LogNotStarted(string subject) => Format(nameof(LogNotStarted), subject);

    public static string RunnerWindowTitle(string label) => Format(nameof(RunnerWindowTitle), label);

    public static string RunnerCancel() => Get(nameof(RunnerCancel));

    public static string RunnerClose() => Get(nameof(RunnerClose));

    public static string RunnerConfirmCancel() => Get(nameof(RunnerConfirmCancel));

    public static string RunnerStarting() => Get(nameof(RunnerStarting));

    public static string RunnerRunning(int done, int total, string current) => Format(nameof(RunnerRunning), done, total, current);

    public static string RunnerCancelling() => Get(nameof(RunnerCancelling));

    public static string RunnerWaitingForElevation() => Get(nameof(RunnerWaitingForElevation));

    public static string RunnerSummaryTitle() => Get(nameof(RunnerSummaryTitle));

    public static string RunnerVersionMismatch(string shellExtVersion, string runnerVersion) =>
        Format(nameof(RunnerVersionMismatch), shellExtVersion, runnerVersion);

    public static string RunnerFatal(string detail) => Format(nameof(RunnerFatal), detail);

    public static string EditorTitle(string fileName) => Format(nameof(EditorTitle), fileName);

    public static string EditorMenuFile() => Get(nameof(EditorMenuFile));

    public static string EditorMenuSave() => Get(nameof(EditorMenuSave));

    public static string EditorMenuReload() => Get(nameof(EditorMenuReload));

    public static string EditorMenuOpenFolder() => Get(nameof(EditorMenuOpenFolder));

    public static string EditorMenuExit() => Get(nameof(EditorMenuExit));

    public static string EditorMenuEdit() => Get(nameof(EditorMenuEdit));

    public static string EditorMenuUndo() => Get(nameof(EditorMenuUndo));

    public static string EditorMenuRedo() => Get(nameof(EditorMenuRedo));

    public static string EditorMenuAddCommand() => Get(nameof(EditorMenuAddCommand));

    public static string EditorMenuAddSubmenu() => Get(nameof(EditorMenuAddSubmenu));

    public static string EditorMenuAddSeparator() => Get(nameof(EditorMenuAddSeparator));

    public static string EditorMenuDuplicate() => Get(nameof(EditorMenuDuplicate));

    public static string EditorMenuDelete() => Get(nameof(EditorMenuDelete));

    public static string EditorMenuMoveUp() => Get(nameof(EditorMenuMoveUp));

    public static string EditorMenuMoveDown() => Get(nameof(EditorMenuMoveDown));

    public static string EditorMenuMoveIn() => Get(nameof(EditorMenuMoveIn));

    public static string EditorMenuMoveOut() => Get(nameof(EditorMenuMoveOut));

    public static string EditorMenuTest() => Get(nameof(EditorMenuTest));

    public static string EditorMenuPreview() => Get(nameof(EditorMenuPreview));

    public static string EditorMenuHelp() => Get(nameof(EditorMenuHelp));

    public static string EditorMenuAbout() => Get(nameof(EditorMenuAbout));

    public static string EditorDeleteKey() => Get(nameof(EditorDeleteKey));

    public static string EditorSeparatorNode() => Get(nameof(EditorSeparatorNode));

    public static string EditorNoLabel() => Get(nameof(EditorNoLabel));

    public static string EditorNothingSelected() => Get(nameof(EditorNothingSelected));

    public static string EditorSeparatorSelected() => Get(nameof(EditorSeparatorSelected));

    public static string EditorIssueLocation() => Get(nameof(EditorIssueLocation));

    public static string EditorIssueProblem() => Get(nameof(EditorIssueProblem));

    public static string EditorIssueDocument() => Get(nameof(EditorIssueDocument));

    public static string EditorStatusSaved() => Get(nameof(EditorStatusSaved));

    public static string EditorStatusUnsaved() => Get(nameof(EditorStatusUnsaved));

    public static string EditorStatusNew() => Get(nameof(EditorStatusNew));

    public static string EditorStatusIssues(int count) => Format(nameof(EditorStatusIssues), count);

    public static string EditorStatusNoIssues() => Get(nameof(EditorStatusNoIssues));

    public static string EditorStatusBackup(string fileName) => Format(nameof(EditorStatusBackup), fileName);

    public static string EditorNewCommandLabel() => Get(nameof(EditorNewCommandLabel));

    public static string EditorNewSubmenuLabel() => Get(nameof(EditorNewSubmenuLabel));

    public static string EditorLoadFailedHeading() => Get(nameof(EditorLoadFailedHeading));

    public static string EditorLoadFailedNotepad() => Get(nameof(EditorLoadFailedNotepad));

    public static string EditorLoadFailedStartOver() => Get(nameof(EditorLoadFailedStartOver));

    public static string EditorLoadFailedStartOverNote() => Get(nameof(EditorLoadFailedStartOverNote));

    public static string EditorLoadFailedExit() => Get(nameof(EditorLoadFailedExit));

    public static string EditorSaveWithIssues(int count) => Format(nameof(EditorSaveWithIssues), count);

    public static string EditorOverwriteChanged() => Get(nameof(EditorOverwriteChanged));

    public static string EditorCommentsWarning() => Get(nameof(EditorCommentsWarning));

    public static string EditorSaveFailed(string detail) => Format(nameof(EditorSaveFailed), detail);

    public static string EditorReadFailed(string detail) => Format(nameof(EditorReadFailed), detail);

    public static string EditorReloadDiscard() => Get(nameof(EditorReloadDiscard));

    public static string EditorChangedOnDisk() => Get(nameof(EditorChangedOnDisk));

    public static string EditorChangedOnDiskDirty() => Get(nameof(EditorChangedOnDiskDirty));

    public static string EditorSaveChanges(string fileName) => Format(nameof(EditorSaveChanges), fileName);

    public static string EditorAbout(string version, string path) => Format(nameof(EditorAbout), version, path);

    public static string EditorFieldLabel() => Get(nameof(EditorFieldLabel));

    public static string EditorGroupMatch() => Get(nameof(EditorGroupMatch));

    public static string EditorGroupCommand() => Get(nameof(EditorGroupCommand));

    public static string EditorMatchTargets() => Get(nameof(EditorMatchTargets));

    public static string EditorMatchFiles() => Get(nameof(EditorMatchFiles));

    public static string EditorMatchFolders() => Get(nameof(EditorMatchFolders));

    public static string EditorMatchBackground() => Get(nameof(EditorMatchBackground));

    public static string EditorMatchDrives() => Get(nameof(EditorMatchDrives));

    public static string EditorMatchTargetsHint() => Get(nameof(EditorMatchTargetsHint));

    public static string EditorMatchExtensions() => Get(nameof(EditorMatchExtensions));

    public static string EditorMatchExtensionsHint() => Get(nameof(EditorMatchExtensionsHint));

    public static string EditorMatchPatterns() => Get(nameof(EditorMatchPatterns));

    public static string EditorMatchPatternsHint() => Get(nameof(EditorMatchPatternsHint));

    public static string EditorMatchExclude() => Get(nameof(EditorMatchExclude));

    public static string EditorMatchMode() => Get(nameof(EditorMatchMode));

    public static string EditorMatchModeAll() => Get(nameof(EditorMatchModeAll));

    public static string EditorMatchModeAny() => Get(nameof(EditorMatchModeAny));

    public static string EditorMatchMinCount() => Get(nameof(EditorMatchMinCount));

    public static string EditorMatchMaxCount() => Get(nameof(EditorMatchMaxCount));

    public static string EditorCommandKind() => Get(nameof(EditorCommandKind));

    public static string EditorRunsProgram() => Get(nameof(EditorRunsProgram));

    public static string EditorCommandProgram() => Get(nameof(EditorCommandProgram));

    public static string EditorCommandBrowse() => Get(nameof(EditorCommandBrowse));

    public static string EditorCommandArguments() => Get(nameof(EditorCommandArguments));

    public static string EditorCommandInsertVariable() => Get(nameof(EditorCommandInsertVariable));

    public static string EditorCommandWorkingDirectory() => Get(nameof(EditorCommandWorkingDirectory));

    public static string EditorCommandWorkingDirectoryHint() => Get(nameof(EditorCommandWorkingDirectoryHint));

    public static string EditorCommandRunMode() => Get(nameof(EditorCommandRunMode));

    public static string EditorRunModeTrack() => Get(nameof(EditorRunModeTrack));

    public static string EditorRunModeLaunch() => Get(nameof(EditorRunModeLaunch));

    public static string EditorCommandMultiSelect() => Get(nameof(EditorCommandMultiSelect));

    public static string EditorMultiSequential() => Get(nameof(EditorMultiSequential));

    public static string EditorMultiParallel() => Get(nameof(EditorMultiParallel));

    public static string EditorMultiAllAtOnce() => Get(nameof(EditorMultiAllAtOnce));

    public static string EditorCommandMaxParallelism() => Get(nameof(EditorCommandMaxParallelism));

    public static string EditorCommandWindow() => Get(nameof(EditorCommandWindow));

    public static string EditorWindowNormal() => Get(nameof(EditorWindowNormal));

    public static string EditorWindowHidden() => Get(nameof(EditorWindowHidden));

    public static string EditorWindowMinimized() => Get(nameof(EditorWindowMinimized));

    public static string EditorWindowMaximized() => Get(nameof(EditorWindowMaximized));

    public static string EditorCommandEncoding() => Get(nameof(EditorCommandEncoding));

    public static string EditorEncodingOem() => Get(nameof(EditorEncodingOem));

    public static string EditorEncodingUtf8() => Get(nameof(EditorEncodingUtf8));

    public static string EditorEncodingAnsi() => Get(nameof(EditorEncodingAnsi));

    public static string EditorCommandRunAs() => Get(nameof(EditorCommandRunAs));

    public static string EditorCommandNoWindow() => Get(nameof(EditorCommandNoWindow));

    public static string EditorCommandShellExecute() => Get(nameof(EditorCommandShellExecute));

    public static string EditorCommandCloseOnSuccess() => Get(nameof(EditorCommandCloseOnSuccess));

    public static string EditorCommandShowOutput() => Get(nameof(EditorCommandShowOutput));

    public static string EditorCommandVerb() => Get(nameof(EditorCommandVerb));

    public static string EditorBrowseFilter() => Get(nameof(EditorBrowseFilter));

    public static string EditorVarPath() => Get(nameof(EditorVarPath));

    public static string EditorVarDir() => Get(nameof(EditorVarDir));

    public static string EditorVarName() => Get(nameof(EditorVarName));

    public static string EditorVarNameNoExt() => Get(nameof(EditorVarNameNoExt));

    public static string EditorVarExt() => Get(nameof(EditorVarExt));

    public static string EditorVarPathNoExt() => Get(nameof(EditorVarPathNoExt));

    public static string EditorVarAllPaths() => Get(nameof(EditorVarAllPaths));

    public static string EditorPreviewTitle() => Get(nameof(EditorPreviewTitle));

    public static string EditorPreviewRightClickOn() => Get(nameof(EditorPreviewRightClickOn));

    public static string EditorPreviewItems() => Get(nameof(EditorPreviewItems));

    public static string EditorPreviewBackground() => Get(nameof(EditorPreviewBackground));

    public static string EditorPreviewAddFiles() => Get(nameof(EditorPreviewAddFiles));

    public static string EditorPreviewAddFolder() => Get(nameof(EditorPreviewAddFolder));

    public static string EditorPreviewRemove() => Get(nameof(EditorPreviewRemove));

    public static string EditorPreviewClear() => Get(nameof(EditorPreviewClear));

    public static string EditorPreviewPathColumn() => Get(nameof(EditorPreviewPathColumn));

    public static string EditorPreviewKindColumn() => Get(nameof(EditorPreviewKindColumn));

    public static string EditorKindFile() => Get(nameof(EditorKindFile));

    public static string EditorKindFolder() => Get(nameof(EditorKindFolder));

    public static string EditorKindBackground() => Get(nameof(EditorKindBackground));

    public static string EditorKindDrive() => Get(nameof(EditorKindDrive));

    public static string EditorPreviewDropHint() => Get(nameof(EditorPreviewDropHint));

    public static string EditorPreviewNeedOneFolder() => Get(nameof(EditorPreviewNeedOneFolder));

    public static string EditorPreviewNoMenu() => Get(nameof(EditorPreviewNoMenu));

    public static string EditorPreviewInvalid() => Get(nameof(EditorPreviewInvalid));

    public static string EditorPreviewProgram() => Get(nameof(EditorPreviewProgram));

    public static string EditorPreviewArguments() => Get(nameof(EditorPreviewArguments));

    public static string EditorPreviewFolder() => Get(nameof(EditorPreviewFolder));

    public static string EditorPreviewRun() => Get(nameof(EditorPreviewRun));

    public static string EditorPreviewClose() => Get(nameof(EditorPreviewClose));

    public static string EditorPreviewSelectCommand() => Get(nameof(EditorPreviewSelectCommand));

    public static string EditorPreviewProcessCount(int count) => Format(nameof(EditorPreviewProcessCount), count);

    public static string EditorPreviewElevated(int count) => Format(nameof(EditorPreviewElevated), count);

    public static string EditorPreviewRunnerMissing(string path) => Format(nameof(EditorPreviewRunnerMissing), path);

    public static string EditorPreviewRunFailed(string detail) => Format(nameof(EditorPreviewRunFailed), detail);

    public static string RestarterUsage() => Get(nameof(RestarterUsage));

    public static string RestarterMissingValue(string option) => Format(nameof(RestarterMissingValue), option);

    public static string RestarterMissingCommand() => Get(nameof(RestarterMissingCommand));

    public static string RestarterUnknownOption(string option) => Format(nameof(RestarterUnknownOption), option);

    public static string RestarterElevated() => Get(nameof(RestarterElevated));

    public static string RestarterSaving() => Get(nameof(RestarterSaving));

    public static string RestarterSaved(int windows, int tabs) => Format(nameof(RestarterSaved), windows, tabs);

    public static string RestarterSaveTimeout() => Get(nameof(RestarterSaveTimeout));

    public static string RestarterSaveFailed(string detail) => Format(nameof(RestarterSaveFailed), detail);

    public static string RestarterStopping() => Get(nameof(RestarterStopping));

    public static string RestarterNotRunning() => Get(nameof(RestarterNotRunning));

    public static string RestarterStopFailed(string detail) => Format(nameof(RestarterStopFailed), detail);

    public static string RestarterStopped() => Get(nameof(RestarterStopped));

    public static string RestarterHolders(string fileName) => Format(nameof(RestarterHolders), fileName);

    public static string RestarterHolder(string name, int processId) => Format(nameof(RestarterHolder), name, processId);

    public static string RestarterRunning(string commandLine) => Format(nameof(RestarterRunning), commandLine);

    public static string RestarterCommandFailed(int exitCode) => Format(nameof(RestarterCommandFailed), exitCode);

    public static string RestarterCommandStartFailed(string detail) => Format(nameof(RestarterCommandStartFailed), detail);

    public static string RestarterCancelled() => Get(nameof(RestarterCancelled));

    public static string RestarterCameBack() => Get(nameof(RestarterCameBack));

    public static string RestarterStarting() => Get(nameof(RestarterStarting));

    public static string RestarterStartedDirectly() => Get(nameof(RestarterStartedDirectly));

    public static string RestarterShellTimeout(int seconds) => Format(nameof(RestarterShellTimeout), seconds);

    public static string RestarterRestoring(int count) => Format(nameof(RestarterRestoring), count);

    public static string RestarterAlreadyOpen(int count) => Format(nameof(RestarterAlreadyOpen), count);

    public static string RestarterWindowFailed(string folder, string detail) => Format(nameof(RestarterWindowFailed), folder, detail);

    public static string RestarterTabAsWindow(string folder) => Format(nameof(RestarterTabAsWindow), folder);

    public static string RestarterRestoreTimeout() => Get(nameof(RestarterRestoreTimeout));

    public static string RestarterDone() => Get(nameof(RestarterDone));

    public static string RestarterDoneWithWarnings() => Get(nameof(RestarterDoneWithWarnings));

    public static string RestarterNoWindow() => Get(nameof(RestarterNoWindow));

    public static string RestarterTabNotNavigated(string folder) => Format(nameof(RestarterTabNotNavigated), folder);

    public static string ValidationIdNeedsVersion() => Get(nameof(ValidationIdNeedsVersion));

    public static string ValidationIdEmpty() => Get(nameof(ValidationIdEmpty));

    public static string ValidationIdDuplicate(string id) => Format(nameof(ValidationIdDuplicate), id);

    public static string ManagerUsage() => Get(nameof(ManagerUsage));

    public static string ManagerUnknownCommand(string command) => Format(nameof(ManagerUnknownCommand), command);

    public static string ManagerMissingFile(string command) => Format(nameof(ManagerMissingFile), command);

    public static string ManagerDllMissing(string path) => Format(nameof(ManagerDllMissing), path);

    public static string ManagerRegistrationOk(string path) => Format(nameof(ManagerRegistrationOk), path);

    public static string ManagerRegistered(string path) => Format(nameof(ManagerRegistered), path);

    public static string ManagerRegisteredElsewhere(string path, string version) => Format(nameof(ManagerRegisteredElsewhere), path, version);

    public static string ManagerRegisterFailed(string path, int exitCode) => Format(nameof(ManagerRegisterFailed), path, exitCode);

    public static string ManagerOtherCopyKept(string path) => Format(nameof(ManagerOtherCopyKept), path);

    public static string ManagerUnregistered() => Get(nameof(ManagerUnregistered));

    public static string ManagerTemplateMissing(string path) => Format(nameof(ManagerTemplateMissing), path);

    public static string ManagerTemplateCreated() => Get(nameof(ManagerTemplateCreated));

    public static string ManagerTemplateReplaced() => Get(nameof(ManagerTemplateReplaced));

    public static string ManagerTemplateMerged(string ids) => Format(nameof(ManagerTemplateMerged), ids);

    public static string ManagerTemplateUpToDate() => Get(nameof(ManagerTemplateUpToDate));

    public static string ManagerBackupSaved(string path) => Format(nameof(ManagerBackupSaved), path);

    public static string ManagerNoConfig() => Get(nameof(ManagerNoConfig));

    public static string ManagerRestored(string path) => Format(nameof(ManagerRestored), path);

    public static string ManagerReset() => Get(nameof(ManagerReset));

    public static string ManagerPreviousSaved(string path) => Format(nameof(ManagerPreviousSaved), path);

    public static string ManagerInvalidFile(string path, string detail) => Format(nameof(ManagerInvalidFile), path, detail);

    public static string ManagerFailed(string detail) => Format(nameof(ManagerFailed), detail);

    public static string EditorMenuTools() => Get(nameof(EditorMenuTools));

    public static string EditorMenuCheck() => Get(nameof(EditorMenuCheck));

    public static string EditorMenuBackup() => Get(nameof(EditorMenuBackup));

    public static string EditorMenuRestore() => Get(nameof(EditorMenuRestore));

    public static string EditorMenuReset() => Get(nameof(EditorMenuReset));

    public static string EditorResetConfirm() => Get(nameof(EditorResetConfirm));

    public static string EditorReplaceUnsaved() => Get(nameof(EditorReplaceUnsaved));

    public static string EditorManagerMissing(string path) => Format(nameof(EditorManagerMissing), path);

    public static string EditorManagerTimeout() => Get(nameof(EditorManagerTimeout));

    public static string EditorConfigFilter() => Get(nameof(EditorConfigFilter));

    public static string ManagerTemplateNewer(string version) => Format(nameof(ManagerTemplateNewer), version);

    public static string ManagerTemplateSkipped() => Get(nameof(ManagerTemplateSkipped));

    public static string FfmpegFound(string path, string version) => Format(nameof(FfmpegFound), path, version);

    public static string FfmpegAllEncoders() => Get(nameof(FfmpegAllEncoders));

    public static string FfmpegMissingEncoders(string encoders) => Format(nameof(FfmpegMissingEncoders), encoders);

    public static string FfmpegNotFound() => Get(nameof(FfmpegNotFound));

    public static string FfmpegNotAnswering(string path) => Format(nameof(FfmpegNotAnswering), path);

    public static string FfmpegNoWinget() => Get(nameof(FfmpegNoWinget));

    public static string FfmpegWingetRunning(string package) => Format(nameof(FfmpegWingetRunning), package);

    public static string FfmpegWingetFailed(int exitCode) => Format(nameof(FfmpegWingetFailed), exitCode);

    public static string FfmpegDownloading(string version, string url) => Format(nameof(FfmpegDownloading), version, url);

    public static string FfmpegBadVersion(string text) => Format(nameof(FfmpegBadVersion), text);

    public static string FfmpegBadChecksum() => Get(nameof(FfmpegBadChecksum));

    public static string FfmpegNoExeInArchive() => Get(nameof(FfmpegNoExeInArchive));

    public static string FfmpegInstalled(string folder) => Format(nameof(FfmpegInstalled), folder);

    public static string FfmpegPathAdded(string folder) => Format(nameof(FfmpegPathAdded), folder);

    public static string FfmpegPathRemoved(string folder) => Format(nameof(FfmpegPathRemoved), folder);

    public static string FfmpegPathNothingToAdd(string folder) => Format(nameof(FfmpegPathNothingToAdd), folder);

    public static string EditorMenuFfmpeg() => Get(nameof(EditorMenuFfmpeg));

    public static string EditorFfmpegTitle() => Get(nameof(EditorFfmpegTitle));

    public static string EditorFfmpegHeading() => Get(nameof(EditorFfmpegHeading));

    public static string EditorFfmpegLicence() => Get(nameof(EditorFfmpegLicence));

    public static string EditorFfmpegWinget() => Get(nameof(EditorFfmpegWinget));

    public static string EditorFfmpegWingetNote() => Get(nameof(EditorFfmpegWingetNote));

    public static string EditorFfmpegDownload() => Get(nameof(EditorFfmpegDownload));

    public static string EditorFfmpegDownloadNote() => Get(nameof(EditorFfmpegDownloadNote));

    public static string EditorFfmpegAddToPath() => Get(nameof(EditorFfmpegAddToPath));

    public static string EditorFfmpegNotNow() => Get(nameof(EditorFfmpegNotNow));

    public static string EditorFfmpegWorking() => Get(nameof(EditorFfmpegWorking));

    public static string EditorManagerCancelled() => Get(nameof(EditorManagerCancelled));

    public static string EditorFfmpegShadowed() => Get(nameof(EditorFfmpegShadowed));

    public static string EditorCancel() => Get(nameof(EditorCancel));

    public static string ToolCopyPath() => Get(nameof(ToolCopyPath));

    public static string ToolCopyPathNote() => Get(nameof(ToolCopyPathNote));

    public static string ToolCopyName() => Get(nameof(ToolCopyName));

    public static string ToolCopyNameNote() => Get(nameof(ToolCopyNameNote));

    public static string ToolCopyUncPath() => Get(nameof(ToolCopyUncPath));

    public static string ToolCopyUncPathNote() => Get(nameof(ToolCopyUncPathNote));

    public static string ToolCopyContent() => Get(nameof(ToolCopyContent));

    public static string ToolCopyContentNote(int maxMegabytes) => Format(nameof(ToolCopyContentNote), maxMegabytes);

    public static string ToolNotAFile(string path) => Format(nameof(ToolNotAFile), path);

    public static string ToolNotText(string path) => Format(nameof(ToolNotText), path);

    public static string ToolItemFailed(string path, string message) => Format(nameof(ToolItemFailed), path, message);

    public static string ToolContentTooLarge(int maxMegabytes) => Format(nameof(ToolContentTooLarge), maxMegabytes);

    public static string ToolNothingCopied() => Get(nameof(ToolNothingCopied));

    public static string ToolClipboardBusy() => Get(nameof(ToolClipboardBusy));

    public static string ToolClipboardFailed(string message) => Format(nameof(ToolClipboardFailed), message);


    public static string SplitPromptOne(string name, string size) => Format(nameof(SplitPromptOne), name, size);

    public static string SplitPromptMany(int count) => Format(nameof(SplitPromptMany), count);

    public static string SplitPromptHint() => Get(nameof(SplitPromptHint));

    public static string SplitPromptInvalid(string text) => Format(nameof(SplitPromptInvalid), text);

    public static string SplitPromptOk() => Get(nameof(SplitPromptOk));


    public static string ToolChecksum() => Get(nameof(ToolChecksum));

    public static string ToolChecksumNote() => Get(nameof(ToolChecksumNote));

    public static string ToolVerifyChecksum() => Get(nameof(ToolVerifyChecksum));

    public static string ToolVerifyChecksumNote() => Get(nameof(ToolVerifyChecksumNote));

    public static string ToolSplit() => Get(nameof(ToolSplit));

    public static string ToolSplitNote() => Get(nameof(ToolSplitNote));

    public static string ToolJoin() => Get(nameof(ToolJoin));

    public static string ToolJoinNote() => Get(nameof(ToolJoinNote));

    public static string ToolCountLines() => Get(nameof(ToolCountLines));

    public static string ToolCountLinesNote() => Get(nameof(ToolCountLinesNote));

    public static string ToolVerifyClipboard() => Get(nameof(ToolVerifyClipboard));

    public static string ToolVerifyNothing() => Get(nameof(ToolVerifyNothing));

    public static string ToolVerifyMismatch(string algorithm, string source, string expected, string actual) => Format(nameof(ToolVerifyMismatch), algorithm, source, expected, actual);

    public static string ToolVerifyMatches(string algorithm, string source) => Format(nameof(ToolVerifyMatches), algorithm, source);

    public static string ToolVerifyListEmpty(string name) => Format(nameof(ToolVerifyListEmpty), name);

    public static string ToolVerifyEntryMatch(string name) => Format(nameof(ToolVerifyEntryMatch), name);

    public static string ToolVerifyEntryMismatch(string name) => Format(nameof(ToolVerifyEntryMismatch), name);

    public static string ToolVerifyEntryMissing(string name) => Format(nameof(ToolVerifyEntryMissing), name);

    public static string ToolVerifyListFailed(int bad, int total) => Format(nameof(ToolVerifyListFailed), bad, total);

    public static string ToolVerifyListPassed(int total) => Format(nameof(ToolVerifyListPassed), total);

    public static string ToolSplitTooSmall(string size) => Format(nameof(ToolSplitTooSmall), size);

    public static string ToolAlreadyExists(string name) => Format(nameof(ToolAlreadyExists), name);

    public static string ToolFileChanged(string name) => Format(nameof(ToolFileChanged), name);

    public static string ToolSplitDone(int count, string size, string first, string last) => Format(nameof(ToolSplitDone), count, size, first, last);

    public static string ToolSplitChecksum(string name) => Format(nameof(ToolSplitChecksum), name);

    public static string ToolJoinNotAPart(string name) => Format(nameof(ToolJoinNotAPart), name);

    public static string ToolJoinFirstMissing(string name) => Format(nameof(ToolJoinFirstMissing), name);

    public static string ToolJoinGap(string name) => Format(nameof(ToolJoinGap), name);

    public static string ToolJoinDone(int count, string name, string size) => Format(nameof(ToolJoinDone), count, name, size);

    public static string ToolJoinMismatch(string name) => Format(nameof(ToolJoinMismatch), name);

    public static string ToolJoinNotVerified() => Get(nameof(ToolJoinNotVerified));

    public static string ToolLineCount(string count) => Format(nameof(ToolLineCount), count);

    public static string ToolVerifyListUnreadable(int count) => Format(nameof(ToolVerifyListUnreadable), count);

    public static string ToolCopyInternetPath() => Get(nameof(ToolCopyInternetPath));

    public static string ToolCopyInternetPathNote() => Get(nameof(ToolCopyInternetPathNote));

    public static string ToolAppendClipboard() => Get(nameof(ToolAppendClipboard));

    public static string ToolAppendClipboardNote() => Get(nameof(ToolAppendClipboardNote));

    public static string ToolDuplicate() => Get(nameof(ToolDuplicate));

    public static string ToolDuplicateNote() => Get(nameof(ToolDuplicateNote));

    public static string ToolFolderSize() => Get(nameof(ToolFolderSize));

    public static string ToolFolderSizeNote() => Get(nameof(ToolFolderSizeNote));

    public static string ToolNotAFolder(string path) => Format(nameof(ToolNotAFolder), path);

    public static string ToolClipboardEmpty() => Get(nameof(ToolClipboardEmpty));

    public static string ToolAppendFailed() => Get(nameof(ToolAppendFailed));

    public static string ToolDuplicateDone(string name) => Format(nameof(ToolDuplicateDone), name);

    public static string ToolFolderSizeResult(string size, string bytes, string files, string folders) => Format(nameof(ToolFolderSizeResult), size, bytes, files, folders);

    public static string ToolFolderSizeUnreadable(int count) => Format(nameof(ToolFolderSizeUnreadable), count);

    public static string PromptOk() => Get(nameof(PromptOk));

    public static string PromptCancel() => Get(nameof(PromptCancel));

    public static string ToolAppendUnmappable(string path) => Format(nameof(ToolAppendUnmappable), path);

    public static string ToolClipboardTooLarge(int millions) => Format(nameof(ToolClipboardTooLarge), millions);

    public static string ToolPackToFolder() => Get(nameof(ToolPackToFolder));

    public static string ToolPackToFolderNote() => Get(nameof(ToolPackToFolderNote));

    public static string ToolNewFolder() => Get(nameof(ToolNewFolder));

    public static string ToolNewFolderNote() => Get(nameof(ToolNewFolderNote));

    public static string ToolRunWithParameters() => Get(nameof(ToolRunWithParameters));

    public static string ToolRunWithParametersNote() => Get(nameof(ToolRunWithParametersNote));

    public static string ToolUnpackFolder() => Get(nameof(ToolUnpackFolder));

    public static string ToolUnpackFolderNote() => Get(nameof(ToolUnpackFolderNote));

    public static string ToolDeleteEmptyFolders() => Get(nameof(ToolDeleteEmptyFolders));

    public static string ToolDeleteEmptyFoldersNote() => Get(nameof(ToolDeleteEmptyFoldersNote));

    public static string ToolDeletePermanently() => Get(nameof(ToolDeletePermanently));

    public static string ToolDeletePermanentlyNote() => Get(nameof(ToolDeletePermanentlyNote));

    public static string ToolCopyTitle() => Get(nameof(ToolCopyTitle));

    public static string ToolCopyTitleNote() => Get(nameof(ToolCopyTitleNote));

    public static string ToolWrongKind(string path) => Format(nameof(ToolWrongKind), path);

    public static string ToolNameInvalid(string name) => Format(nameof(ToolNameInvalid), name);

    public static string ToolNameTaken(string name) => Format(nameof(ToolNameTaken), name);

    public static string ToolUnpackLink(string name) => Format(nameof(ToolUnpackLink), name);

    public static string ToolUnpackTaken(string name, string names) => Format(nameof(ToolUnpackTaken), name, names);

    public static string ToolNoTitle(string path) => Format(nameof(ToolNoTitle), path);

    public static string ToolPackFailed() => Get(nameof(ToolPackFailed));

    public static string ToolUnpackFailed() => Get(nameof(ToolUnpackFailed));

    public static string ToolDeleteFoldersFailed() => Get(nameof(ToolDeleteFoldersFailed));

    public static string ToolRunFailed(string path, string message) => Format(nameof(ToolRunFailed), path, message);

    public static string PromptPackQuestion(int count) => Format(nameof(PromptPackQuestion), count);

    public static string PromptPackQuestionOne(string name) => Format(nameof(PromptPackQuestionOne), name);

    public static string PromptNewFolderQuestion(string folder) => Format(nameof(PromptNewFolderQuestion), folder);

    public static string PromptNewFolderName() => Get(nameof(PromptNewFolderName));

    public static string PromptRunQuestion(string name) => Format(nameof(PromptRunQuestion), name);

    public static string PromptRunHint() => Get(nameof(PromptRunHint));

    public static string PromptRunOk() => Get(nameof(PromptRunOk));

    public static string ConfirmDeleteEmpty(int count, string list) => Format(nameof(ConfirmDeleteEmpty), count, list);

    public static string NoEmptyFolders(string name) => Format(nameof(NoEmptyFolders), name);

    public static string ConfirmDeletePermanently(int count, string list) => Format(nameof(ConfirmDeletePermanently), count, list);

    public static string ConfirmListMore(int count) => Format(nameof(ConfirmListMore), count);

    public static string ToolPackDifferentFolders() => Get(nameof(ToolPackDifferentFolders));

    public static string ToolRunOneProgram() => Get(nameof(ToolRunOneProgram));

    public static string ToolUnpackUndone(string name, string message) => Format(nameof(ToolUnpackUndone), name, message);

    public static string ToolNothingRemoved() => Get(nameof(ToolNothingRemoved));

    public static string ToolEmptyFoldersRemoved(int count) => Format(nameof(ToolEmptyFoldersRemoved), count);

    public static string ToolWikiImport() => Get(nameof(ToolWikiImport));

    public static string ToolWikiImportNote() => Get(nameof(ToolWikiImportNote));

    public static string WikiAddressNotUrl(string text) => Format(nameof(WikiAddressNotUrl), text);

    public static string WikiAddressNoPath() => Get(nameof(WikiAddressNoPath));

    public static string WikiAddressFirstSegment(string segment) => Format(nameof(WikiAddressFirstSegment), segment);

    public static string WikiAddressBadSegment(string segment) => Format(nameof(WikiAddressBadSegment), segment);

    public static string WikiAddressDeviceName(string segment) => Format(nameof(WikiAddressDeviceName), segment);

    public static string WikiSettingsInvalid(string path, string message) => Format(nameof(WikiSettingsInvalid), path, message);

    public static string WikiSettingsNotSaved(string path, string message) => Format(nameof(WikiSettingsNotSaved), path, message);

    public static string WikiKeyRejected(string message) => Format(nameof(WikiKeyRejected), message);

    public static string WikiFolderHidden(string path) => Format(nameof(WikiFolderHidden), path);

    public static string WikiRedirected(string baseUrl, string location) => Format(nameof(WikiRedirected), baseUrl, location);

    public static string WikiPageNotMarkdown(string url, string contentType) => Format(nameof(WikiPageNotMarkdown), url, contentType);

    public static string WikiUploadForbidden(string name, string message) => Format(nameof(WikiUploadForbidden), name, message);

    public static string WikiUploadFailed(string name, string message) => Format(nameof(WikiUploadFailed), name, message);

    public static string WikiUploadNotStored(string name) => Format(nameof(WikiUploadNotStored), name);

    public static string WikiNoAnswer() => Get(nameof(WikiNoAnswer));

    public static string WikiRefused(string message, int code) => Format(nameof(WikiRefused), message, code);

    public static string WikiError(string message) => Format(nameof(WikiError), message);

    public static string WikiNotWikiJs2(string baseUrl, string status) => Format(nameof(WikiNotWikiJs2), baseUrl, status);

    public static string WikiUnreachable(string baseUrl, string message) => Format(nameof(WikiUnreachable), baseUrl, message);

    public static string WikiFoundImages(int found, int missing) => Format(nameof(WikiFoundImages), found, missing);

    public static string WikiImageMissing(string target) => Format(nameof(WikiImageMissing), target);

    public static string WikiFolderCreated(string path, string created) => Format(nameof(WikiFolderCreated), path, created);

    public static string WikiFolderFound(string path) => Format(nameof(WikiFolderFound), path);

    public static string WikiUploaded(string file, string name) => Format(nameof(WikiUploaded), file, name);

    public static string WikiUploadedReplacing(string file, string name) => Format(nameof(WikiUploadedReplacing), file, name);

    public static string WikiPageCreated(string url) => Format(nameof(WikiPageCreated), url);

    public static string WikiPageUpdated(string url) => Format(nameof(WikiPageUpdated), url);

    public static string WikiAskUrl(string name) => Format(nameof(WikiAskUrl), name);

    public static string WikiAskUrlHint() => Get(nameof(WikiAskUrlHint));

    public static string WikiAskKey(string site) => Format(nameof(WikiAskKey), site);

    public static string WikiAskKeyHint() => Get(nameof(WikiAskKeyHint));

    public static string WikiKeyTooShort() => Get(nameof(WikiKeyTooShort));

    public static string WikiConfirmUpdate(string url) => Format(nameof(WikiConfirmUpdate), url);

    public static string WikiSavedWithMissing(string url, int count, string list) => Format(nameof(WikiSavedWithMissing), url, count, list);

    public static string WikiOneFile() => Get(nameof(WikiOneFile));

    public static string ToolCopyToFolder() => Get(nameof(ToolCopyToFolder));

    public static string ToolCopyToFolderNote() => Get(nameof(ToolCopyToFolderNote));

    public static string ToolMoveToFolder() => Get(nameof(ToolMoveToFolder));

    public static string ToolMoveToFolderNote() => Get(nameof(ToolMoveToFolderNote));

    public static string ToolCreateLink() => Get(nameof(ToolCreateLink));

    public static string ToolCreateLinkNote() => Get(nameof(ToolCreateLinkNote));

    public static string ToolJoinFolders() => Get(nameof(ToolJoinFolders));

    public static string ToolJoinFoldersNote() => Get(nameof(ToolJoinFoldersNote));

    public static string ToolChangeFolderIcon() => Get(nameof(ToolChangeFolderIcon));

    public static string ToolChangeFolderIconNote() => Get(nameof(ToolChangeFolderIconNote));

    public static string ToolRestoreFolderIcon() => Get(nameof(ToolRestoreFolderIcon));

    public static string ToolRestoreFolderIconNote() => Get(nameof(ToolRestoreFolderIconNote));

    public static string PickCopyTitle(int count) => Format(nameof(PickCopyTitle), count);

    public static string PickCopyTitleOne(string name) => Format(nameof(PickCopyTitleOne), name);

    public static string PickMoveTitle(int count) => Format(nameof(PickMoveTitle), count);

    public static string PickMoveTitleOne(string name) => Format(nameof(PickMoveTitleOne), name);

    public static string PickLinkTitle(int count) => Format(nameof(PickLinkTitle), count);

    public static string PickLinkTitleOne(string name) => Format(nameof(PickLinkTitleOne), name);

    public static string PickCopyOk() => Get(nameof(PickCopyOk));

    public static string PickMoveOk() => Get(nameof(PickMoveOk));

    public static string PickLinkOk() => Get(nameof(PickLinkOk));

    public static string ToolFileOperationFailed(string message) => Format(nameof(ToolFileOperationFailed), message);

    public static string ToolJoinTwoOrMore() => Get(nameof(ToolJoinTwoOrMore));

    public static string ToolJoinDifferentFolders() => Get(nameof(ToolJoinDifferentFolders));

    public static string ToolJoinLink(string name) => Format(nameof(ToolJoinLink), name);

    public static string PromptJoinQuestion(int count) => Format(nameof(PromptJoinQuestion), count);

    public static string ToolJoinNotRemoved() => Get(nameof(ToolJoinNotRemoved));

    public static string ToolLinkLoop(string name) => Format(nameof(ToolLinkLoop), name);

    public static string ToolLinkNeedsPrivilege(string name) => Format(nameof(ToolLinkNeedsPrivilege), name);

    public static string ToolLinkNotLocal(string name) => Format(nameof(ToolLinkNotLocal), name);

    public static string ToolLinkFailed(string name, string message) => Format(nameof(ToolLinkFailed), name, message);

    public static string ToolLinksFailed() => Get(nameof(ToolLinksFailed));

    public static string ToolIconFailed(string name, string message) => Format(nameof(ToolIconFailed), name, message);

    public static string ToolIconsFailed() => Get(nameof(ToolIconsFailed));

    public static string ToolLinkTooLong() => Get(nameof(ToolLinkTooLong));

    public static string ToolPickerFailed(string message) => Format(nameof(ToolPickerFailed), message);

    public static string ToolIconSpecialFolder(string name) => Format(nameof(ToolIconSpecialFolder), name);

    public static string ToolIconFileMissing(string path) => Format(nameof(ToolIconFileMissing), path);

    public static string ToolIconDrive(string name) => Format(nameof(ToolIconDrive), name);

    public static string ToolLinkPlaceNotLocal(string name) => Format(nameof(ToolLinkPlaceNotLocal), name);

    public static string ToolAttributes() => Get(nameof(ToolAttributes));

    public static string ToolAttributesNote() => Get(nameof(ToolAttributesNote));

    public static string ToolChangeTime() => Get(nameof(ToolChangeTime));

    public static string ToolChangeTimeNote() => Get(nameof(ToolChangeTimeNote));

    public static string ToolOrganizeIntoFolders() => Get(nameof(ToolOrganizeIntoFolders));

    public static string ToolOrganizeIntoFoldersNote() => Get(nameof(ToolOrganizeIntoFoldersNote));

    public static string ToolAdvancedDelete() => Get(nameof(ToolAdvancedDelete));

    public static string ToolAdvancedDeleteNote() => Get(nameof(ToolAdvancedDeleteNote));

    public static string ToolNeedsEditor(string fileName) => Format(nameof(ToolNeedsEditor), fileName);

    public static string ToolEditorFailed(string message) => Format(nameof(ToolEditorFailed), message);

    public static string ToolWindowSelectedOne(string name) => Format(nameof(ToolWindowSelectedOne), name);

    public static string ToolWindowSelected(int count, string names) => Format(nameof(ToolWindowSelected), count, names);

    public static string ToolWindowApply() => Get(nameof(ToolWindowApply));

    public static string ToolWindowClose() => Get(nameof(ToolWindowClose));

    public static string ToolWindowWorking() => Get(nameof(ToolWindowWorking));

    public static string ToolWindowInside() => Get(nameof(ToolWindowInside));

    public static string ToolWindowNotChanged() => Get(nameof(ToolWindowNotChanged));

    public static string AttributeReadOnly() => Get(nameof(AttributeReadOnly));

    public static string AttributeHidden() => Get(nameof(AttributeHidden));

    public static string AttributeSystem() => Get(nameof(AttributeSystem));

    public static string AttributeArchive() => Get(nameof(AttributeArchive));

    public static string AttributesHint() => Get(nameof(AttributesHint));

    public static string TimeCreated() => Get(nameof(TimeCreated));

    public static string TimeModified() => Get(nameof(TimeModified));

    public static string TimeAccessed() => Get(nameof(TimeAccessed));

    public static string TimeNow() => Get(nameof(TimeNow));

    public static string TimeHint() => Get(nameof(TimeHint));

    public static string OrganizeByDate() => Get(nameof(OrganizeByDate));

    public static string OrganizeByExtension() => Get(nameof(OrganizeByExtension));

    public static string OrganizeDateLabel() => Get(nameof(OrganizeDateLabel));

    public static string OrganizeDateModified() => Get(nameof(OrganizeDateModified));

    public static string OrganizeDateCreated() => Get(nameof(OrganizeDateCreated));

    public static string OrganizeDateTaken() => Get(nameof(OrganizeDateTaken));

    public static string OrganizeFoldersLabel() => Get(nameof(OrganizeFoldersLabel));

    public static string OrganizeFolderYear(string example) => Format(nameof(OrganizeFolderYear), example);

    public static string OrganizeFolderYearMonth(string example) => Format(nameof(OrganizeFolderYearMonth), example);

    public static string OrganizeFolderYearMonthDay(string example) => Format(nameof(OrganizeFolderYearMonthDay), example);

    public static string OrganizeFolderYearThenMonth(string example) => Format(nameof(OrganizeFolderYearThenMonth), example);

    public static string OrganizeFileColumn() => Get(nameof(OrganizeFileColumn));

    public static string OrganizeFolderColumn() => Get(nameof(OrganizeFolderColumn));

    public static string OrganizeNoExtension() => Get(nameof(OrganizeNoExtension));

    public static string OrganizeReadingDates() => Get(nameof(OrganizeReadingDates));

    public static string OrganizeSummary(int files, int folders) => Format(nameof(OrganizeSummary), files, folders);

    public static string OrganizeNothing() => Get(nameof(OrganizeNothing));

    public static string OrganizeMove() => Get(nameof(OrganizeMove));

    public static string OrganizeNotMoved() => Get(nameof(OrganizeNotMoved));

    public static string DeletePatternsLabel() => Get(nameof(DeletePatternsLabel));

    public static string DeletePatternsHint() => Get(nameof(DeletePatternsHint));

    public static string DeleteFiles() => Get(nameof(DeleteFiles));

    public static string DeleteFolders() => Get(nameof(DeleteFolders));

    public static string DeleteSubfolders() => Get(nameof(DeleteSubfolders));

    public static string DeleteFind() => Get(nameof(DeleteFind));

    public static string DeletePathColumn() => Get(nameof(DeletePathColumn));

    public static string DeleteSizeColumn() => Get(nameof(DeleteSizeColumn));

    public static string DeleteModifiedColumn() => Get(nameof(DeleteModifiedColumn));

    public static string DeleteFolderSize() => Get(nameof(DeleteFolderSize));

    public static string DeleteRecycle() => Get(nameof(DeleteRecycle));

    public static string DeleteConfirm(int count) => Format(nameof(DeleteConfirm), count);

    public static string DeleteFound(int files, string size, int folders) => Format(nameof(DeleteFound), files, size, folders);

    public static string DeleteNothingFound() => Get(nameof(DeleteNothingFound));

    public static string DeleteUnreadable(int count) => Format(nameof(DeleteUnreadable), count);

    public static string DeleteSearching() => Get(nameof(DeleteSearching));

    public static string DeletePatternsEmpty() => Get(nameof(DeletePatternsEmpty));

    public static string DeletePatternInvalid(string pattern) => Format(nameof(DeletePatternInvalid), pattern);

    public static string DeleteNotDeleted() => Get(nameof(DeleteNotDeleted));

    public static string ToolRecycleFailed(string message) => Format(nameof(ToolRecycleFailed), message);

    public static string OrganizeUnreadable(int count) => Format(nameof(OrganizeUnreadable), count);

    public static string ToolMoveFailed(string message) => Format(nameof(ToolMoveFailed), message);

    public static string ToolAdvancedRename() => Get(nameof(ToolAdvancedRename));

    public static string ToolAdvancedRenameNote() => Get(nameof(ToolAdvancedRenameNote));

    public static string ToolFindReplace() => Get(nameof(ToolFindReplace));

    public static string ToolFindReplaceNote() => Get(nameof(ToolFindReplaceNote));

    public static string ToolRegexInvalid(string message) => Format(nameof(ToolRegexInvalid), message);

    public static string ToolRegexTimeout() => Get(nameof(ToolRegexTimeout));

    public static string ToolRenameFailed(string message) => Format(nameof(ToolRenameFailed), message);

    public static string RenameProblemInvalid() => Get(nameof(RenameProblemInvalid));

    public static string RenameProblemDuplicate() => Get(nameof(RenameProblemDuplicate));

    public static string RenameProblemExists() => Get(nameof(RenameProblemExists));

    public static string RenameProblemSwap() => Get(nameof(RenameProblemSwap));

    public static string RenameTokenUnknown(string token) => Format(nameof(RenameTokenUnknown), token);

    public static string RenameDateFormatInvalid(string format) => Format(nameof(RenameDateFormatInvalid), format);

    public static string ReplaceFindEmpty() => Get(nameof(ReplaceFindEmpty));

    public static string ReplaceTooLarge(int megabytes) => Format(nameof(ReplaceTooLarge), megabytes);

    public static string ReplaceUnmappable() => Get(nameof(ReplaceUnmappable));

    public static string ReplaceNotText() => Get(nameof(ReplaceNotText));

    public static string SearchFindLabel() => Get(nameof(SearchFindLabel));

    public static string SearchReplaceLabel() => Get(nameof(SearchReplaceLabel));

    public static string SearchMatchCase() => Get(nameof(SearchMatchCase));

    public static string SearchRegularExpression() => Get(nameof(SearchRegularExpression));

    public static string RenameNameLabel() => Get(nameof(RenameNameLabel));

    public static string RenameExtensionLabel() => Get(nameof(RenameExtensionLabel));

    public static string RenameTokens() => Get(nameof(RenameTokens));

    public static string RenameNumbersLabel() => Get(nameof(RenameNumbersLabel));

    public static string RenameStart() => Get(nameof(RenameStart));

    public static string RenameStep() => Get(nameof(RenameStep));

    public static string RenameDigits() => Get(nameof(RenameDigits));

    public static string RenameCaseLabel() => Get(nameof(RenameCaseLabel));

    public static string RenameCaseUnchanged() => Get(nameof(RenameCaseUnchanged));

    public static string RenameCaseLower() => Get(nameof(RenameCaseLower));

    public static string RenameCaseUpper() => Get(nameof(RenameCaseUpper));

    public static string RenameCaseTitle() => Get(nameof(RenameCaseTitle));

    public static string RenameOldColumn() => Get(nameof(RenameOldColumn));

    public static string RenameNewColumn() => Get(nameof(RenameNewColumn));

    public static string RenameProblemColumn() => Get(nameof(RenameProblemColumn));

    public static string RenameSummary(int changes, int total) => Format(nameof(RenameSummary), changes, total);

    public static string RenameProblems(int count) => Format(nameof(RenameProblems), count);

    public static string RenameButton() => Get(nameof(RenameButton));

    public static string RenameNotRenamed() => Get(nameof(RenameNotRenamed));

    public static string ReplaceWholeWord() => Get(nameof(ReplaceWholeWord));

    public static string ReplaceFilesLabel() => Get(nameof(ReplaceFilesLabel));

    public static string ReplaceSubfolders() => Get(nameof(ReplaceSubfolders));

    public static string ReplaceBackup() => Get(nameof(ReplaceBackup));

    public static string ReplaceHint(int megabytes) => Format(nameof(ReplaceHint), megabytes);

    public static string ReplaceFileColumn() => Get(nameof(ReplaceFileColumn));

    public static string ReplaceMatchesColumn() => Get(nameof(ReplaceMatchesColumn));

    public static string ReplaceNoteColumn() => Get(nameof(ReplaceNoteColumn));

    public static string ReplacePreview() => Get(nameof(ReplacePreview));

    public static string ReplaceButton() => Get(nameof(ReplaceButton));

    public static string ReplaceSearching() => Get(nameof(ReplaceSearching));

    public static string ReplaceSummary(int matches, int files) => Format(nameof(ReplaceSummary), matches, files);

    public static string ReplaceNothing() => Get(nameof(ReplaceNothing));

    public static string ReplaceUnreadable(int count) => Format(nameof(ReplaceUnreadable), count);

    public static string ReplaceStale() => Get(nameof(ReplaceStale));

    public static string ReplaceConfirm(int matches, int files) => Format(nameof(ReplaceConfirm), matches, files);

    public static string ReplaceNotReplaced() => Get(nameof(ReplaceNotReplaced));

    public static string RenameRegexTooSlow() => Get(nameof(RenameRegexTooSlow));

    public static string RenameProblemTooLong() => Get(nameof(RenameProblemTooLong));

    public static string ReplaceLink() => Get(nameof(ReplaceLink));

    public static string ToolDeleteLocked() => Get(nameof(ToolDeleteLocked));

    public static string ToolDeleteLockedNote() => Get(nameof(ToolDeleteLockedNote));

    public static string LockedNotFound() => Get(nameof(LockedNotFound));

    public static string LockedSearching() => Get(nameof(LockedSearching));

    public static string LockedProgramColumn() => Get(nameof(LockedProgramColumn));

    public static string LockedIdColumn() => Get(nameof(LockedIdColumn));

    public static string LockedKindColumn() => Get(nameof(LockedKindColumn));

    public static string LockedHint() => Get(nameof(LockedHint));

    public static string LockedCloseAndDelete() => Get(nameof(LockedCloseAndDelete));

    public static string LockedDelete() => Get(nameof(LockedDelete));

    public static string LockedAtRestart() => Get(nameof(LockedAtRestart));

    public static string LockedSearchFailed(string message) => Format(nameof(LockedSearchFailed), message);

    public static string LockedTooMany(int count) => Format(nameof(LockedTooMany), count);

    public static string LockedNone() => Get(nameof(LockedNone));

    public static string LockedHolders() => Get(nameof(LockedHolders));

    public static string LockedKindService() => Get(nameof(LockedKindService));

    public static string LockedKindExplorer() => Get(nameof(LockedKindExplorer));

    public static string LockedKindCritical() => Get(nameof(LockedKindCritical));

    public static string LockedKindConsole() => Get(nameof(LockedKindConsole));

    public static string LockedKindProgram() => Get(nameof(LockedKindProgram));

    public static string LockedClosing() => Get(nameof(LockedClosing));

    public static string LockedStillUsed() => Get(nameof(LockedStillUsed));

    public static string LockedEndQuestion(string name) => Format(nameof(LockedEndQuestion), name);

    public static string LockedScheduled(int count) => Format(nameof(LockedScheduled), count);

    public static string LockedArgumentsTooLong() => Get(nameof(LockedArgumentsTooLong));

    public static string LockedElevationFailed(string message) => Format(nameof(LockedElevationFailed), message);

    public static string LockedNotScheduled() => Get(nameof(LockedNotScheduled));

    public static string CryptNotOurs() => Get(nameof(CryptNotOurs));

    public static string CryptWrongPassword() => Get(nameof(CryptWrongPassword));

    public static string CryptAlreadyEncrypted() => Get(nameof(CryptAlreadyEncrypted));

    public static string ToolEncrypt() => Get(nameof(ToolEncrypt));

    public static string ToolEncryptNote() => Get(nameof(ToolEncryptNote));

    public static string ToolDecrypt() => Get(nameof(ToolDecrypt));

    public static string ToolDecryptNote() => Get(nameof(ToolDecryptNote));

    public static string EncryptWithPassword() => Get(nameof(EncryptWithPassword));

    public static string EncryptWithEfs() => Get(nameof(EncryptWithEfs));

    public static string EncryptEfsHint() => Get(nameof(EncryptEfsHint));

    public static string EncryptPasswordHint() => Get(nameof(EncryptPasswordHint));

    public static string CryptPasswordLabel() => Get(nameof(CryptPasswordLabel));

    public static string CryptConfirmLabel() => Get(nameof(CryptConfirmLabel));

    public static string EncryptDeleteOriginals() => Get(nameof(EncryptDeleteOriginals));

    public static string EncryptButton() => Get(nameof(EncryptButton));

    public static string CryptPasswordEmpty() => Get(nameof(CryptPasswordEmpty));

    public static string CryptPasswordsDiffer() => Get(nameof(CryptPasswordsDiffer));

    public static string CryptPasswordShort(int length) => Format(nameof(CryptPasswordShort), length);

    public static string EncryptNotEncrypted() => Get(nameof(EncryptNotEncrypted));

    public static string DecryptSearching() => Get(nameof(DecryptSearching));

    public static string DecryptFound(int password, int efs) => Format(nameof(DecryptFound), password, efs);

    public static string DecryptNothing() => Get(nameof(DecryptNothing));

    public static string DecryptDeleteEncrypted() => Get(nameof(DecryptDeleteEncrypted));

    public static string DecryptHint() => Get(nameof(DecryptHint));

    public static string DecryptButton() => Get(nameof(DecryptButton));

    public static string DecryptNotDecrypted() => Get(nameof(DecryptNotDecrypted));

    public static string ToolSyncFolders() => Get(nameof(ToolSyncFolders));

    public static string ToolSyncFoldersNote() => Get(nameof(ToolSyncFoldersNote));

    public static string SyncFrom() => Get(nameof(SyncFrom));

    public static string SyncTo() => Get(nameof(SyncTo));

    public static string SyncBrowse() => Get(nameof(SyncBrowse));

    public static string SyncSwap() => Get(nameof(SyncSwap));

    public static string SyncExtras() => Get(nameof(SyncExtras));

    public static string SyncHint() => Get(nameof(SyncHint));

    public static string SyncCompare() => Get(nameof(SyncCompare));

    public static string SyncActionColumn() => Get(nameof(SyncActionColumn));

    public static string SyncButton() => Get(nameof(SyncButton));

    public static string SyncComparing() => Get(nameof(SyncComparing));

    public static string SyncNew() => Get(nameof(SyncNew));

    public static string SyncChanged() => Get(nameof(SyncChanged));

    public static string SyncNewerThere() => Get(nameof(SyncNewerThere));

    public static string SyncExtra() => Get(nameof(SyncExtra));

    public static string SyncSame() => Get(nameof(SyncSame));

    public static string SyncSummary(int added, int changed, int newerThere, int extra) => Format(nameof(SyncSummary), added, changed, newerThere, extra);

    public static string SyncConfirm(int copies, int replaced, int extras) => Format(nameof(SyncConfirm), copies, replaced, extras);

    public static string SyncNotDone() => Get(nameof(SyncNotDone));

    public static string SyncNotCompared() => Get(nameof(SyncNotCompared));

    public static string SyncNoDestination() => Get(nameof(SyncNoDestination));

    public static string SyncDestinationMissing(string path) => Format(nameof(SyncDestinationMissing), path);

    public static string SyncInside() => Get(nameof(SyncInside));

    public static string SyncKindDiffers() => Get(nameof(SyncKindDiffers));

    public static string CryptDestinationTaken(string path) => Format(nameof(CryptDestinationTaken), path);

    public static string CryptOriginalKept(string message) => Format(nameof(CryptOriginalKept), message);

    public static string CryptEncryptedKept(string message) => Format(nameof(CryptEncryptedKept), message);

    public static string SyncSizeDiffers() => Get(nameof(SyncSizeDiffers));

    public static string SyncNothingToCopy() => Get(nameof(SyncNothingToCopy));

    public static string SyncLeftAlone(int count) => Format(nameof(SyncLeftAlone), count);

    public static string SyncLinkLeft() => Get(nameof(SyncLinkLeft));

    public static string SyncNotFullPath(string path) => Format(nameof(SyncNotFullPath), path);

    public static string SyncStopped() => Get(nameof(SyncStopped));

    public static string WikiGatewayNoAnswer(string baseUrl, string status) => Format(nameof(WikiGatewayNoAnswer), baseUrl, status);

    public static string WikiSavedWithoutAnswer(string reason) => Format(nameof(WikiSavedWithoutAnswer), reason);

    public static string WikiSaveUnknown(string reason, string url) => Format(nameof(WikiSaveUnknown), reason, url);

    public static string ConfigNewer(int version, int current, string detail) => Format(nameof(ConfigNewer), version, current, detail);

    public static string ConfigNewerOnDisk(int version, int current) => Format(nameof(ConfigNewerOnDisk), version, current);

    public static string EditorConfigNewer(string message) => Format(nameof(EditorConfigNewer), message);

    public static string EditorLoadFailedFactory() => Get(nameof(EditorLoadFailedFactory));

    public static string EditorLoadFailedFactoryNote() => Get(nameof(EditorLoadFailedFactoryNote));

    public static string EditorConfigNewerHeading() => Get(nameof(EditorConfigNewerHeading));

    public static string EditorMenuWikiKey() => Get(nameof(EditorMenuWikiKey));

    public static string WikiKeyTitle() => Get(nameof(WikiKeyTitle));

    public static string WikiKeyAddress() => Get(nameof(WikiKeyAddress));

    public static string WikiKeyKey() => Get(nameof(WikiKeyKey));

    public static string WikiKeyHelp() => Get(nameof(WikiKeyHelp));

    public static string WikiKeySave() => Get(nameof(WikiKeySave));

    public static string WikiKeyForget() => Get(nameof(WikiKeyForget));

    public static string WikiKeyChecking(string site) => Format(nameof(WikiKeyChecking), site);

    public static string WikiKeyStored(string site) => Format(nameof(WikiKeyStored), site);

    public static string WikiKeyNoneStored(string site) => Format(nameof(WikiKeyNoneStored), site);

    public static string WikiKeySaved(string site) => Format(nameof(WikiKeySaved), site);

    public static string WikiKeyNotSaved(string message) => Format(nameof(WikiKeyNotSaved), message);

    public static string WikiKeyForgetConfirm(string site) => Format(nameof(WikiKeyForgetConfirm), site);

    public static string WikiKeyEnvironment() => Get(nameof(WikiKeyEnvironment));

    public static string WikiKeySavedEnvironment(string site) => Format(nameof(WikiKeySavedEnvironment), site);

    public static string WikiKeyForgetConfirmEnvironment(string site) => Format(nameof(WikiKeyForgetConfirmEnvironment), site);
}
