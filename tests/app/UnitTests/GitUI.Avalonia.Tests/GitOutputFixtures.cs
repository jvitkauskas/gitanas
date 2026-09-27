namespace GitUI.AvaloniaTests;

/// <summary>Outputs of git 2.55 (with the configuration of the Gitanas viewer) for the tests of the diff appearances.</summary>
internal static class GitOutputFixtures
{
    /// <summary>git diff --color=always --word-diff=color (with the colors and diff.wordregex of PatchHighlightService.GetGitCommandConfiguration).</summary>
    public const string WordDiff
        = "\u001b[1mdiff --git a/calc.cs b/calc.cs\u001b[m\n"
        + "\u001b[1mindex 6f984ee..94a7cb1 100644\u001b[m\n"
        + "\u001b[1m--- a/calc.cs\u001b[m\n"
        + "\u001b[1m+++ b/calc.cs\u001b[m\n"
        + "\u001b[36m@@ -2,12 +2,13 @@\u001b[m \u001b[mnamespace Demo;\u001b[m\n"
        + "\n"
        + "public sealed class Calculator\u001b[m\n"
        + "{\u001b[m\n"
        + "\u001b[7;31m    // Multiplies the value.\u001b[m\n"
        + "    public int Compute(int value)\u001b[m\n"
        + "    {\u001b[m\n"
        + "        int result = value * \u001b[7;31m2\u001b[m\u001b[7;32m3\u001b[m;\n"
        + "        return result + Offset;\u001b[m\n"
        + "    }\u001b[m\n"
        + "\n"
        + "    private const int Offset = \u001b[7;31m1\u001b[m\u001b[7;32m10;\u001b[m\n"
        + "\n"
        + "\u001b[7;32m    public int Twice(int value) => Compute(value) * 2\u001b[m;\n"
        + "}\u001b[m\n";

    /// <summary>git diff --color=always of the same change.</summary>
    public const string Patch
        = "\u001b[1mdiff --git a/calc.cs b/calc.cs\u001b[m\n"
        + "\u001b[1mindex 6f984ee..94a7cb1 100644\u001b[m\n"
        + "\u001b[1m--- a/calc.cs\u001b[m\n"
        + "\u001b[1m+++ b/calc.cs\u001b[m\n"
        + "\u001b[36m@@ -2,12 +2,13 @@\u001b[m \u001b[mnamespace Demo;\u001b[m\n"
        + " \u001b[m\n"
        + " public sealed class Calculator\u001b[m\n"
        + " {\u001b[m\n"
        + "\u001b[7;31m-    // Multiplies the value.\u001b[m\n"
        + "     public int Compute(int value)\u001b[m\n"
        + "     {\u001b[m\n"
        + "\u001b[7;31m-        int result = value * 2;\u001b[m\n"
        + "\u001b[7;32m+\u001b[m\u001b[7;32m        int result = value * 3;\u001b[m\n"
        + "         return result + Offset;\u001b[m\n"
        + "     }\u001b[m\n"
        + " \u001b[m\n"
        + "\u001b[7;31m-    private const int Offset = 1;\u001b[m\n"
        + "\u001b[7;32m+\u001b[m\u001b[7;32m    private const int Offset = 10;\u001b[m\n"
        + "\u001b[7;32m+\u001b[m\n"
        + "\u001b[7;32m+\u001b[m\u001b[7;32m    public int Twice(int value) => Compute(value) * 2;\u001b[m\n"
        + " }\u001b[m\n";

    /// <summary>git diff-tree --cc --color=always of a merge that resolved a conflict.</summary>
    public const string CombinedDiff
        = "\u001b[1mdiff --cc calc.cs\u001b[m\n"
        + "\u001b[1mindex 94a7cb1,c3dba92..0f5c6dc\u001b[m\n"
        + "\u001b[1m--- a/calc.cs\u001b[m\n"
        + "\u001b[1m+++ b/calc.cs\u001b[m\n"
        + "\u001b[36m@@@ -2,9 -2,10 +2,9 @@@\u001b[m \u001b[mnamespace Demo\u001b[m\n"
        + "  \u001b[m\n"
        + "  public sealed class Calculator\u001b[m\n"
        + "  {\u001b[m\n"
        + "\u001b[7;31m -    // Multiplies the value.\u001b[m\n"
        + "      public int Compute(int value)\u001b[m\n"
        + "      {\u001b[m\n"
        + "\u001b[7;31m-         int result = value * 3;\u001b[m\n"
        + "\u001b[7;31m -        int result = value * 4;\u001b[m\n"
        + "\u001b[7;32m++        int result = value * 5;\u001b[m\n"
        + "          return result + Offset;\u001b[m\n"
        + "      }\u001b[m\n"
        + "  \u001b[m\n";

    /// <summary>git grep --line-number --show-function --color=always -h --context=1 (with GrepHighlightService.GetGitCommandConfiguration).</summary>
    public const string Grep
        = "3=\u001b[2;7;37mpublic sealed class Calculator\u001b[m\n"
        + "4-{\n"
        + "5:    public int Compute(int \u001b[1;7;31mvalue\u001b[m)\n"
        + "6-    {\n"
        + "7:        int result = \u001b[1;7;31mvalue\u001b[m * 5;\n"
        + "8-        return result + Offset;\n"
        + "--\n"
        + "12-\n"
        + "13:    public int Twice(int \u001b[1;7;31mvalue\u001b[m) => Compute(\u001b[1;7;31mvalue\u001b[m) * 2;\n"
        + "14-}\n";

    /// <summary>A hunk of git difftool --tool=difftastic with DFT_WIDTH=200 (from GitUI.Tests/Editor/Diff/SampleDifftastic.diff).</summary>
    public const string Difftastic
        = "\u001b[1mC:\\Users\\ejgo\\AppData\\Local\\Temp/git-blob-b26504/FileViewer.cs\u001b[0m\u001b[2m --- 2/16 --- C#\u001b[0m\n"
        + "\u001b[2m45 \u001b[0m                                                                                                 \u001b[2m46 \u001b[0m\n"
        + "\u001b[2m46 \u001b[0m        \u001b[1mprivate\u001b[0m \u001b[1mreadonly\u001b[0m \u001b[1mAsyncLoader\u001b[0m _async;                                                     \u001b[2m47 \u001b[0m        \u001b[1mprivate\u001b[0m \u001b[1mreadonly\u001b[0m \u001b[1mAsyncLoader\u001b[0m _async;\n"
        + "\u001b[2m47 \u001b[0m        \u001b[1mprivate\u001b[0m \u001b[1mreadonly\u001b[0m \u001b[1mIFullPathResolver\u001b[0m _fullPathResolver;                                    \u001b[2m48 \u001b[0m        \u001b[1mprivate\u001b[0m \u001b[1mreadonly\u001b[0m \u001b[1mIFullPathResolver\u001b[0m _fullPathResolver;\n"
        + "\u001b[2m.. \u001b[0m                                                                                                 \u001b[32;1m49 \u001b[0m        \u001b[32;1mprivate\u001b[0m \u001b[32;1mreadonly\u001b[0m \u001b[32;1mTaskDialogPage\u001b[0m \u001b[32m_NO_TRANSLATE_resetSelectedLinesConfirmationDialog\u001b[0m\u001b[32m;\u001b[0m\n"
        + "\u001b[2m.. \u001b[0m                                                                                                 \u001b[32;1m50 \u001b[0m        \u001b[32;1mprivate\u001b[0m \u001b[32;1mreadonly\u001b[0m \u001b[32;1mContinuousScrollEventManager\u001b[0m \u001b[32m_continuousScrollEventManager\u001b[0m\u001b[32m;\u001b[0m\n"
        + "\u001b[2m.. \u001b[0m                                                                                                 \u001b[32;1m51 \u001b[0m\n"
        + "\u001b[2m.. \u001b[0m                                                                                                 \u001b[32;1m52 \u001b[0m        \u001b[32;3m// Cache for the configuration of a difftastic difftool\u001b[0m\n"
        + "\u001b[2m.. \u001b[0m                                                                                                 \u001b[32;1m53 \u001b[0m        \u001b[32;1mprivate\u001b[0m \u001b[32;1mreadonly\u001b[0m \u001b[32;1mDictionary\u001b[0m\u001b[32;1m<\u001b[0m\u001b[32;1mstring\u001b[0m\u001b[32m,\u001b[0m \u001b[32;1mbool\u001b[0m\u001b[32;1m>\u001b[0m \u001b[32m_difftasticCmdCache\u001b[0m \u001b[32;1m=\u001b[0m \u001b[32m[\u001b[0m\u001b[32m]\u001b[0m\u001b[32m;\u001b[0m\n"
        + "\u001b[2m.. \u001b[0m                                                                                                 \u001b[32;1m54 \u001b[0m\n"
        + "\u001b[2m48 \u001b[0m        \u001b[1mprivate\u001b[0m \u001b[1mViewMode\u001b[0m _viewMode;                                                              \u001b[2m55 \u001b[0m        \u001b[1mprivate\u001b[0m \u001b[1mViewMode\u001b[0m _viewMode;\n"
        + "\u001b[2m49 \u001b[0m        \u001b[1mprivate\u001b[0m Encoding\u001b[1m?\u001b[0m _encoding;                                                             \u001b[2m56 \u001b[0m        \u001b[1mprivate\u001b[0m Encoding\u001b[1m?\u001b[0m _encoding;\n"
        + "\u001b[2m50 \u001b[0m        \u001b[1mprivate\u001b[0m \u001b[1mFunc\u001b[0m\u001b[1m<\u001b[0m\u001b[1mTask\u001b[0m\u001b[1m>\u001b[0m\u001b[1m?\u001b[0m _deferShowFunc;                                                      \u001b[2m57 \u001b[0m        \u001b[1mprivate\u001b[0m \u001b[1mFunc\u001b[0m\u001b[1m<\u001b[0m\u001b[1mTask\u001b[0m\u001b[1m>\u001b[0m\u001b[1m?\u001b[0m _deferShowFunc;\n"
        + "\u001b[31;1m51 \u001b[0m        \u001b[31;1mprivate\u001b[0m \u001b[31;1mreadonly\u001b[0m \u001b[31;1mContinuousScrollEventManager\u001b[0m \u001b[31m_continuousScrollEventManager\u001b[0m\u001b[31m;\u001b[0m             \u001b[2m.. \u001b[0m\n"
        + "\u001b[2m52 \u001b[0m        \u001b[1mprivate\u001b[0m FileStatusItem\u001b[1m?\u001b[0m _viewItem;                                                       \u001b[2m58 \u001b[0m        \u001b[1mprivate\u001b[0m FileStatusItem\u001b[1m?\u001b[0m _viewItem;\n"
        + "\u001b[31;1m53 \u001b[0m        \u001b[31;1mprivate\u001b[0m \u001b[31;1mreadonly\u001b[0m \u001b[31;1mTaskDialogPage\u001b[0m \u001b[31m_NO_TRANSLATE_resetSelectedLinesConfirmationDialog\u001b[0m\u001b[31m;\u001b[0m      \u001b[2m.. \u001b[0m\n"
        + "\u001b[2m54 \u001b[0m                                                                                                 \u001b[2m59 \u001b[0m\n"
        + "\u001b[2m55 \u001b[0m        [GeneratedRegex(\u001b[35m@\"warning: .*has type .* expected .*\"\u001b[0m, RegexOptions.ExplicitCapture)]    \u001b[2m60 \u001b[0m        [GeneratedRegex(\u001b[35m@\"warning: .*has type .* expected .*\"\u001b[0m, RegexOptions.ExplicitCapture)]\n"
        + "\u001b[2m56 \u001b[0m        \u001b[1mprivate\u001b[0m \u001b[1mstatic\u001b[0m \u001b[1mpartial\u001b[0m \u001b[1mRegex\u001b[0m FileModeWarningRegex();                                     \u001b[2m61 \u001b[0m        \u001b[1mprivate\u001b[0m \u001b[1mstatic\u001b[0m \u001b[1mpartial\u001b[0m \u001b[1mRegex\u001b[0m FileModeWarningRegex();\n";

    /// <summary>git range-diff --color=always of a commit changed on another branch.</summary>
    public const string RangeDiff
        = "\u001b[7;31m1:  cfa3f3e \u001b[m\u001b[33m!\u001b[m\u001b[7;32m 1:  f0627de\u001b[m\u001b[33m change\u001b[m\n"
        + "    \u001b[7m\u001b[36m@@\u001b[m \u001b[mcalc.cs: namespace Demo;\u001b[m\n"
        + "          }\u001b[m\n"
        + "      \u001b[m\n"
        + "    \u001b[7;31m -    private const int Offset = 1;\u001b[m\n"
        + "    \u001b[7m\u001b[7;31m-\u001b[m\u001b[2;7;32m+    private const int Offset = 10;\u001b[m\n"
        + "    \u001b[7m\u001b[7;32m+\u001b[m\u001b[7;92m+    private const int Offset = 100;\u001b[m\n"
        + "    \u001b[7;32m +\u001b[m\n"
        + "    \u001b[7;32m +    public int Twice(int value) => Compute(value) * 2;\u001b[m\n"
        + "      }\u001b[m\n";
}
