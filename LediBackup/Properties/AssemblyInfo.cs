/////////////////////////////////////////////////////////////////////////////
//    LediBackup: Copyright (C) 2020 Dr. Dirk Lellinger
//    All rights reserved.
//    This file is licensed to you under the MIT license.
//    See the LICENSE file in the project root for more information.
/////////////////////////////////////////////////////////////////////////////

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;

//
// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
//
[assembly: AssemblyTitle("LediBackup")]
[assembly: AssemblyDescription("Backup program using NTFS hard links")]
[assembly: AssemblyConfiguration("REVID: $REVID$, BRANCH: $BRANCH$, DATE: $REVDATE$")]
[assembly: AssemblyCompany("https://github.com/lellid")]
[assembly: AssemblyProduct("LediBackup")]
[assembly: AssemblyCopyright("(C) Dr. Dirk Lellinger 2020-$YEAR$")]
[assembly: AssemblyTrademark("(C) Dr. Dirk Lellinger 2020-$YEAR$")]
[assembly: AssemblyCulture("")]

// The following line is neccessary if the assembly contains themes for custom wpf controls
[assembly: ThemeInfo(ResourceDictionaryLocation.SourceAssembly, ResourceDictionaryLocation.SourceAssembly)]

//
// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers
// by using the '*' as shown below:

[assembly: AssemblyVersion("$MAJORVERSION$.$MINORVERSION$.$REVNUM$.$DIRTY$")]
[assembly: AssemblyFileVersion("$MAJORVERSION$.$MINORVERSION$.$REVNUM$.$DIRTY$")]

[assembly: LediBackup.Serialization.SupportsSerializationVersioningAttribute()]


