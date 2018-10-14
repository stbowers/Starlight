#!/bin/sh

compiler="/opt/VulkanSDK/1.0.65.0/x86_64/bin/glslangValidator"

for file in ./*
do
    if [[ ${file: -5} = ".frag" || ${file: -5} = ".vert" ]]
    then
        echo "Compiling glsl file: $file"
        $compiler -V -o "$file.spv" $file
    fi
done