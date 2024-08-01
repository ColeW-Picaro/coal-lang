# F# Compiler Frontend Example

```shell
/compiler
    dotnet new classlib -lang=f# -f=net8.0
    dotnet add package FsLexYacc --package-directory deps --framework net8.0
/cli
    dotnet new console -lang=f# -f=8.0
    dotnet add reference ../compiler/
```

## Debian dependencies

```shell
sudo apt install llvm-16
```
