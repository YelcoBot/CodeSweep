using System;

namespace YelcoBot.CodeSweep.Vsix
{
    internal static class PackageGuids
    {
        public const string guidCodeSweepPackageString = "a2f2b3c4-1111-4222-3333-444455556666";
        public static readonly Guid guidCodeSweepPackage = new(guidCodeSweepPackageString);

        public const string guidCodeSweepPackageCmdSetString = "b3f3c4d5-2222-4333-4444-555566667777";
        public static readonly Guid guidCodeSweepPackageCmdSet = new(guidCodeSweepPackageCmdSetString);
    }

    internal static class PackageIds
    {
        public const int MyMenuGroup = 0x1020;
        public const int CleanupActiveDocumentCommandId = 0x0100;
        public const int CleanupOpenDocumentsCommandId = 0x0200;
        public const int CleanupSolutionCommandId = 0x0300;
        public const int ToggleCleanupOnSaveCommandId = 0x0400;
    }
}
