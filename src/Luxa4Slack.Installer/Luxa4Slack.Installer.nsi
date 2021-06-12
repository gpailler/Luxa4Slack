!include "FileFunc.nsh"
!include WinVer.nsh
!addplugindir /x86-unicode ".\Plugins\LockedList"

;--------------------------------
;General

  ; Used in Add/Remove programs
  !define APPNAME "Luxa4Slack"
  !define COMPANYNAME "Gregoire Pailler"
  !include Versions.nsh
  !define HELPURL "https://github.com/gpailler/Luxa4Slack"
  !define UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}"

  ; Used on finish page and Start Menu
  !define LAUNCH_FINISH_APPNAME "Luxa4Slack.Tray"

  !define ROOT "..\.."
  !ifndef CONFIGURATION
    !define CONFIGURATION "Release"
  !endif
  !define BINARIES "${ROOT}\bin\${CONFIGURATION}"
  !define APPFILE_TRAY "Luxa4Slack.Tray.exe"

  ; Resources
  !define LOGO "graphics\logo.ico"
  !define WELCOME_FINISH_BITMAP "graphics\welcomefinishpage.bmp"

  ; Global configuration flags
  Unicode True
  RequestExecutionLevel user
  ManifestDPIAware true
  SetCompressor /SOLID lzma

  ; Global options
  Name "${APPNAME}"
  Icon "${LOGO}"
  OutFile "${ROOT}\artifacts\Luxa4Slack.Installer-${VERSIONMAJOR}.${VERSIONMINOR}.${VERSIONPATCH}.exe"
  InstallDir "$LOCALAPPDATA\Programs\${APPNAME}"

  ; Retrieve previous installation folder from registry if available
  InstallDirRegKey HKCU "${UNINST_KEY}" "InstallLocation"
;--------------------------------


;--------------------------------
; Init
Function .onInit
  ; Check Windows version
  ${IfNot} ${AtLeastWin8}
    MessageBox MB_OK|MB_ICONSTOP "Windows 8 or later is required in order to run ${APPNAME}."
    Quit
  ${EndIf}
FunctionEnd
;--------------------------------


;--------------------------------
; Modern UI configuration

  ; Use Modern UI interface
  ; https://nsis.sourceforge.io/Docs/Modern%20UI/Readme.html
  !include "MUI2.nsh"

  ; Display a warning if user wants to close the installer before the end
  !define MUI_ABORTWARNING

  ; UI Customization
  !define MUI_ICON "${LOGO}"
  !define MUI_UNICON "${LOGO}"
  !define MUI_WELCOMEFINISHPAGE_BITMAP "${WELCOME_FINISH_BITMAP}"
  !define MUI_UNWELCOMEFINISHPAGE_BITMAP "${WELCOME_FINISH_BITMAP}"
  !define MUI_HEADERIMAGE
  !define MUI_HEADERIMAGE_BITMAP "${NSISDIR}\Contrib\Graphics\Header\nsis3-grey.bmp"

  ; Checkbox to launch the app on exit
  !define MUI_FINISHPAGE_RUN "$INSTDIR\${APPFILE_TRAY}"
  !define MUI_FINISHPAGE_RUN_TEXT "Launch ${LAUNCH_FINISH_APPNAME}"

  ; Do not jump to finish page automatically (debug)
  ; !define MUI_FINISHPAGE_NOAUTOCLOSE

  ; Define MUI pages
  !insertmacro MUI_PAGE_WELCOME
  !insertmacro MUI_PAGE_LICENSE "${ROOT}\LICENSE"
  !insertmacro MUI_PAGE_DIRECTORY
  Page Custom LockedListShow
  !insertmacro MUI_PAGE_INSTFILES
  !insertmacro MUI_PAGE_FINISH

  !insertmacro MUI_UNPAGE_WELCOME
  !insertmacro MUI_UNPAGE_CONFIRM
  !insertmacro MUI_UNPAGE_INSTFILES
  !insertmacro MUI_UNPAGE_FINISH

  ; Languages
  !insertmacro MUI_LANGUAGE "English"
;--------------------------------


;--------------------------------
;Installer Sections

Section -UninstallPrevious
  ; Search previous version in registry
  ReadRegStr $R0 HKCU "${UNINST_KEY}" "UninstallString"
  ${If} $R0 != ""
    DetailPrint "Previous installation detected. Calling uninstaller..."
    ReadRegStr $R1 HKCU "${UNINST_KEY}" "InstallLocation"
    ${If} $R1 != ""
      ; Copy uninstaller in a temp directory, execute it and wait the completion
      CopyFiles /SILENT /FILESONLY "$R0" "$PLUGINSDIR\${APPNAME}-uninstaller.exe"
      ExecWait "$PLUGINSDIR\${APPNAME}-uninstaller.exe _?=$R1 /S"
    ${EndIf}
  ${EndIf}
SectionEnd


Section "install"
  SetOutPath $INSTDIR

  ; Files to include in the installer
  File /r "${BINARIES}\*.exe"
  File /r "${BINARIES}\*.json"
  File /r "${BINARIES}\*.config"
  File /r "${BINARIES}\*.dll"
  File "${LOGO}"

  ; Uninstaller
  WriteUninstaller "$INSTDIR\uninstall.exe"

  ; Start Menu
  CreateDirectory "$SMPROGRAMS\${APPNAME}"
  CreateShortCut "$SMPROGRAMS\${APPNAME}\${LAUNCH_FINISH_APPNAME}.lnk" "$INSTDIR\${APPFILE_TRAY}" "" "$INSTDIR\logo.ico"

  ; Registry information for add/remove programs
  WriteRegStr HKCU "${UNINST_KEY}" "DisplayName" "${APPNAME}"
  WriteRegStr HKCU "${UNINST_KEY}" "UninstallString" "$INSTDIR\uninstall.exe"
  WriteRegStr HKCU "${UNINST_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "${UNINST_KEY}" "DisplayIcon" "$INSTDIR\logo.ico"
  WriteRegStr HKCU "${UNINST_KEY}" "Publisher" "${COMPANYNAME}"
  WriteRegStr HKCU "${UNINST_KEY}" "HelpLink" "${HELPURL}"
  WriteRegStr HKCU "${UNINST_KEY}" "DisplayVersion" "${VERSIONMAJOR}.${VERSIONMINOR}.${VERSIONPATCH}"
  WriteRegDWORD HKCU "${UNINST_KEY}" "VersionMajor" ${VERSIONMAJOR}
  WriteRegDWORD HKCU "${UNINST_KEY}" "VersionMinor" ${VERSIONMINOR}
  WriteRegDWORD HKCU "${UNINST_KEY}" "NoModify" 1
  WriteRegDWORD HKCU "${UNINST_KEY}" "NoRepair" 1

  ; Set the estimated size based on INSTDIR size
  ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
  IntFmt $0 "0x%08X" $0
  WriteRegDWORD HKCU "${UNINST_KEY}" "EstimatedSize" "$0"
SectionEnd


Section "uninstall"
  ; Remove Start Menu launcher
  Delete "$SMPROGRAMS\${APPNAME}\${LAUNCH_FINISH_APPNAME}.lnk"

  ; Remove Start Menu folder if empty
  RmDir "$SMPROGRAMS\${APPNAME}"

  ; Kill running processes
  LockedList::CloseProcess /kill "$INSTDIR\${APPFILE_TRAY}"

  ; Remove files
  RmDir /r $INSTDIR

  ; Remove uninstaller information from the registry
  DeleteRegKey HKCU "${UNINST_KEY}"
SectionEnd


Function LockedListShow
  !insertmacro MUI_HEADER_TEXT "Running applications" "Close running applications to continue."
  LockedList::AddModule "$INSTDIR\${APPFILE_TRAY}"
  LockedList::Dialog /autonext /autoclosesilent "" "Close All"
  Pop $R0
FunctionEnd
