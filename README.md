# LediBackup
(C) Dr. Dirk Lellinger 2020

## What LediBackup can do for you
- Backup your user files to a backup drive. The backup drive must be formatted with the NTFS file system! It can be a hard disk, a flash drive or a network share.
- Efficiently store repeated backups, because files that have not changed, i.e. with the same content and attributes, are linked by hardlinks instead of claiming separate storage space. 
- Can handle the hard link limit of 1021 files of the NTFS file system. After reaching the hard link limit, the file is simply physically copied again (instead of hard linked).
- No special program neccessary to restore your files. Simply copy them back from your backup drive into your working folder.
- No special program required to delete some of the backup folders, if you no longer need them. Simply delete them in Explorer.
- Can handle long file names without changing the registry. 
- You can on a daily basis decide which folder to backup, this will not influence the storage efficiency.

## What LediBackup can not do for you
- LediBackup does not retain alternate file streams of NTFS (additional data that are associcated with the main file)
- LediBackup does not retain the creation time, and last access time. But it does maintain the last write time, and this is the most important time stamp, the time that is displayed in explorer!
- LediBackup can follow symbolic links, but it can not detect circular paths (links that form a closed loop).  
- LediBackup can not backup nor restore your system files. It is intended to backup your user files. 


## How it works

This is how it works for one file:

### Secure backup mode

In the simplest case (secure backup mode), there are three files involved:
- The original file (the file to be backuped)
- The backup file (the file with the same name, on the backup drive)
- A central content file (see explanation below)

The process steps are as follows:
- The original file is opened, and the content of the file is read into a buffer.
- The content of the original file and some of its attributes are hashed, using the SHA256 hash algorithm.
- The calculated hash is used to form the file name of the central content file in the central content directory (`~CCS~`) on the backup drive.
- The original file is copied to the central content file.
- A hardlinked is created, linking the central content file to the backup file.

### Fast backup mode

Reading the file content and hashing the content are time consuming steps.
In order to speed up the backup when you repeatedly backup your files, there
is in fast backup mode a forth file included, the central name file. The name of the central name
file is calculated from the SHA256 hash of the original file's full name, using the
central name directory (`~CNS~`). The file is then hard-linked to the central content file.

During the next backup, the original file's name is hashed again. This is fast compared with a full hash of the file's content.
The hash is used again to calculate the name of the central name file.
If the central name file already exists on the backup drive, its length, last write time, and attributes are compared to the original file.
If length, last write time and attributes match, the backup file is simply linked by a hard link to the central name file. This is fast and does not require
to read and hash the content of the file.

There is a catch with this approach: if there is a program that changes the content
of the file, without changing length, last write time, and attributes, that change of the content will not be detected,
and an old version of the file will be stored on the backup drive instead.
Hopefully, such programs are very seldom, and probably malicious, so the altered file
should not be stored on the backup drive anyway!
But if you are unsure about it, do not use this 'speed-up feature'! Instead, always do a full hash of the file content.

## Dos and Don'ts

You should:
- For a directory to backup, use (if possible) a UNC path instead of a network drive letter, 
or at least, use always the same drive letter for the same network path
- For a directory to backup, use the original drive letter instead of a substituted path's drive letter, or at least,
use always the same drive letter for the same substituted path
 

You can safely:
- decide to backup only some of the folders today; this will not diminish the storage efficiency of the following backups.
- delete a directory that contains a backup on the backup drive, if you don't need it anymore.
- delete the central content storage directory `(~CCS~)` on the backup drive (the storage efficiency is slighly diminished for the next backup).
- delete the central names storage directory `(~CNS~)` on the backup drive (the backup speed is somewhat diminished for the next backup).


You should NEVER EVER:
- Never ever work directly with files on the backup drive! Use the files on the
backup drive only to restore lost files by copying them back to your working directory. 
If you change files directly on your backup drive, you have good chances to unintendedly change files from
other backups, too, because the files are hard-linked to each other!

## For developers: Design decisions

### Why the subdirectories level under the central content storage directory (`~CCS~`) is 2

NTFS supports 2^32^ files on a volume. In the first level we have 256 (2^8^) subdirectories,
each subdirectory containing up to 2^8^ files. This are up to 2^16^=65536 total subdirectories in the second level.
For a typical backup directory, we will have some million files. In that case,
in the average we have 15 or more files in one level2 subdirectory, which is a good number.
In the worst case, we will have 2^15^=32768 files in one subdirectory, which is acceptable
(its 2^15^ and not 2^16^ because for each file to backup we have both the backup file itself and the hardlinked file in the central content storage directory).

Would we have chosen 3 subdirectory levels, we could have 2^24^ subdirectories.
For the typical case of some million backup files, that would mean that in the average
we would only have one file per subdirectory. This is wasteful.

### Why is the architecture of the worker structure so complicated?

Reading from a hard disk is fastest, if only one thread is accessing the volume.
Using more that one thread for that task, the reading speed is lessened, instead of increased,
because the disk is busy with repositioning operations. The same is true for writing to a hard disk.

That is why a single task is used for reading content from the volume to backup and a single task is used for writing files to the backup drive.
On the other hand, hashing the content is only calculation, thus multiple tasks (in the limits of the CPU cores) can work
in parallel to hash the content of different files.

For these reasons the architecture of the worker is like this (simplified):

- a single thread is reading the content of the file, then of the next file, and so on. The items are put in a queue, waiting to be hashed.
- multiple threads are trying to get items from the queue, and then calculating the hash of the file contents.
When the hash is calculated, the item is put in the writing queue.
- a single thread gets the item from the writing queue, and writes the content of the file
to the backup medium. Additionally, it creates a hardlink into the central content storage directory. 
