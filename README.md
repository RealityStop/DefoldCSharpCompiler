# DefoldCSharpCompiler

The compiler portion of [DefoldSharp](https://github.com/RealityStop/DefoldSharp), this software is responsible for taking C# code and producing viable [defold](https://defold.com/) code.  It is a custom fork of [CSharp.lua](https://github.com/yanghuan/CSharp.lua), with all changes contained within the `defold` branch, for ease of merging in updates from the base project.  These changes largely consist of the script proxies generation.

Unlike DefoldAPIGen, this compiler IS used for C# defold projects, but most users can skip straight to [releases](https://github.com/RealityStop/DefoldCSharpCompiler/releases).
