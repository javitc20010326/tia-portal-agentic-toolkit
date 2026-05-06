# TIA Portal Openness Requirements

TIA Portal Openness is not a public cloud API and it is not a standalone package that this toolkit can download independently.

It is a local API installed with TIA Portal on the engineering workstation.

## Installation Model

For TIA Portal V20 and similar installer-based versions, Openness is installed through the TIA Portal setup program by selecting the `TIA Portal Openness` option under setup options.

For TIA Portal V21, Siemens documentation describes Openness as an inherent feature of TIA Portal that is automatically installed. The setup creates the local Windows group `Siemens TIA Openness`.

In both cases, an administrator must grant the Windows user access by adding the user to the local group:

```text
Siemens TIA Openness
```

The user must sign out and sign back in after group membership changes.

## Licensing

Siemens documentation states that no separate license is required for the Openness option itself. You still need the normal TIA Portal product/license, such as STEP 7 or WinCC, because Openness automates installed TIA Portal products.

## API Location

Installed Openness versions are discoverable in the Windows registry under:

```text
HKEY_LOCAL_MACHINE\SOFTWARE\Siemens\Automation\Openness
```

The Siemens.Engineering assemblies are installed under the local TIA Portal installation, for example:

```text
C:\Program Files\Siemens\Automation\Portal V20\PublicAPI\V20\Siemens.Engineering.dll
C:\Program Files\Siemens\Automation\Portal V21\PublicAPI\V21\net48\Siemens.Engineering.Base.dll
```

## Runtime Implication

The real Openness execution layer must run on the same Windows machine where TIA Portal is installed, or in an environment that can load the locally installed Siemens.Engineering assemblies and connect to a TIA Portal instance on that machine.

This means the toolkit can be developed anywhere, but real project automation must be tested on a TIA Portal engineering workstation or VM.

## Toolkit Architecture Implication

TIA Portal Openness programming targets .NET Framework 4.8 / Siemens.Engineering assemblies. The MCP protocol layer can be modern .NET, but calls into TIA Portal should be isolated behind a Windows-only Openness bridge that is compiled/tested on the TIA Portal VM.

Recommended final shape:

```text
Codex
  -> MCP stdio server
    -> Openness bridge process/library on the TIA Portal machine
      -> Siemens.Engineering assemblies
        -> TIA Portal project
```
