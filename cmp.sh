diff log-* > ./diff.log
diff -y log-* -W 512  > ./diffy.log

cmp -b log-* > ./diffc.log

if [[ $? -ne 0 ]]
then
    kompare log-* & &> /dev/null
else
    echo 'all OK'
fi
