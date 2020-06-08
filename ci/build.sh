#!/bin/bash
set -x
set -o errexit
DEX_VERSION=$(grep -oP '\[DataEditorX\](.+)\[DataEditorX\]' DataEditorX/readme.txt | sed 's/\[DataEditorX\]//g')

# build
xbuild /p:Configuration=Release /p:OutDir=$PWD/output/ # /p:TargetFrameworkVersion=v4.6 

# zip tool
sed -i '/download.mono-project.com/d' /etc/apt/sources.list /etc/apt/sources.list.d/*
apt update
apt -y install p7zip-full

# zip
mkdir -p dist/releases
cd output
7z a -mx9 ../dist/releases/DataEditorX-$DEX_VERSION.zip ./*
cd ..
cp -rf DataEditorX/readme.txt dist/version.txt
