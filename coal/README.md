# F# Compiler Frontend Example

```
/compiler
    dotnet new classlib -lang=f# -f=netcoreapp3.1
    dotnet add package FsLexYacc --package-directory deps --framework netcoreapp3.1
/cli
    dotnet new console -lang=f# -f=netcoreapp3.1
    dotnet add reference ../compiler/
```

