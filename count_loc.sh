#!/bin/bash

printf "Lines of code (.cs files): "
find ./ -name "*.cs" -exec wc -l {} \; | sed 's/ .*$//' | paste -sd+ - | bc
