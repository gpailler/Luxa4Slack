!include "FileFunc.nsh"

;--------------------------------
;General

  ; Used in Add/Remove programs
  !define APPNAME "Luxa4Slack"
  !define COMPANYNAME "Gregoire Pailler"
  !include Versions.nsh
  !define HELPURL "https://github.com/gpailler/Luxa4Slack"
  !define UNINST_REGISTRY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}"

  ; Used on finish page and Start Menu
  !define LAUNCH_FINISH_APPNAME "Luxa4Slack.Tray"
  !define LAUNCH_FINISH_APPFILE "Luxa4Slack.Tray.exe"

  !define ROOT "..\.."
  !define BINARIES "${ROOT}\bin\Release"

  ; Resources
  !define LOGO "graphics\logo.ico"
  !define WELCOME_FINISH_BITMAP "graphics\welcomefinishpage.bmp"

  ; Global configuration flags
  Unicode True
  RequestExecutionLevel user
  ManifestDPIAware true

  ; Global options
  Name "${APPNAME}"
  Icon "${LOGO}"
  OutFile "${ROOT}\artifacts\Luxa4Slack.Installer-${VERSIONMAJOR}.${VERSIONMINOR}.${VERSIONPATCH}.exe"
  InstallDir "$LOCALAPPDATA\Programs\${APPNAME}"

  ;Get installation folder from registry if available
  InstallDirRegKey HKCU "${UNINST_REGISTRY}" "InstallLocation"

  SetCompressor lzma
;--------------------------------


;--------------------------------
; Modern UI

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
  !define MUI_FINISHPAGE_RUN "$INSTDIR\${LAUNCH_FINISH_APPFILE}"
  !define MUI_FINISHPAGE_RUN_TEXT "Launch ${LAUNCH_FINISH_APPNAME}"

  ; Do not jump to finish page automatically
  ;!define MUI_FINISHPAGE_NOAUTOCLOSE

  ; Define MUI pages
  !insertmacro MUI_PAGE_WELCOME
  !insertmacro MUI_PAGE_LICENSE "${ROOT}\LICENSE"
  !insertmacro MUI_PAGE_DIRECTORY
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
  ReadRegStr $R0 HKCU "${UNINST_REGISTRY}" "QuietUninstallString"

  ${If} $R0 != ""
    DetailPrint "Removing previous installation."
    ExecWait "$R0"
  ${EndIf}
SectionEnd


Section "install"
  SetOutPath $INSTDIR

  ; Files to include in the installer
  File /r "${BINARIES}\*.exe"
  File /r "${BINARIES}\*.exe.config"
  File /r "${BINARIES}\*.dll"
  File "${LOGO}"

  ; Uninstaller
  WriteUninstaller "$INSTDIR\uninstall.exe"

  ; Start Menu
  CreateDirectory "$SMPROGRAMS\${APPNAME}"
  CreateShortCut "$SMPROGRAMS\${APPNAME}\${LAUNCH_FINISH_APPNAME}.lnk" "$INSTDIR\${LAUNCH_FINISH_APPFILE}" "" "$INSTDIR\logo.ico"

  ; Registry information for add/remove programs
  WriteRegStr HKCU "${UNINST_REGISTRY}" "DisplayName" "${APPNAME}"
  WriteRegStr HKCU "${UNINST_REGISTRY}" "UninstallString" "$\"$INSTDIR\uninstall.exe$\""
  WriteRegStr HKCU "${UNINST_REGISTRY}" "QuietUninstallString" "$\"$INSTDIR\uninstall.exe$\" /S"
  WriteRegStr HKCU "${UNINST_REGISTRY}" "InstallLocation" "$\"$INSTDIR$\""
  WriteRegStr HKCU "${UNINST_REGISTRY}" "DisplayIcon" "$\"$INSTDIR\logo.ico$\""
  WriteRegStr HKCU "${UNINST_REGISTRY}" "Publisher" "${COMPANYNAME}"
  WriteRegStr HKCU "${UNINST_REGISTRY}" "HelpLink" "$\"${HELPURL}$\""
  WriteRegStr HKCU "${UNINST_REGISTRY}" "DisplayVersion" "${VERSIONMAJOR}.${VERSIONMINOR}.${VERSIONPATCH}"
  WriteRegDWORD HKCU "${UNINST_REGISTRY}" "VersionMajor" ${VERSIONMAJOR}
  WriteRegDWORD HKCU "${UNINST_REGISTRY}" "VersionMinor" ${VERSIONMINOR}
  WriteRegDWORD HKCU "${UNINST_REGISTRY}" "NoModify" 1
  WriteRegDWORD HKCU "${UNINST_REGISTRY}" "NoRepair" 1

  ; Set the estimated size based on INSTDIR size
  ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
  IntFmt $0 "0x%08X" $0
  WriteRegDWORD HKCU "${UNINST_REGISTRY}" "EstimatedSize" "$0"
SectionEnd


Section "uninstall"
  ; Remove Start Menu launcher
  Delete "$SMPROGRAMS\${APPNAME}\${LAUNCH_FINISH_APPNAME}.lnk"

  ; Remove Start Menu folder if empty
  RmDir "$SMPROGRAMS\${APPNAME}"

  ; Remove files
  RmDir /r $INSTDIR

  ; Remove uninstaller information from the registry
  DeleteRegKey HKCU "${UNINST_REGISTRY}"
sectionEnd
