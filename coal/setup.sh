#!/usr/bin/env bash
cd compiler || exit
dotnet add package FsLexYacc --package-directory deps --framework net8.0
