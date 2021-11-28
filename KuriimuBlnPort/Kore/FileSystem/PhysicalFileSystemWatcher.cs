﻿using System;
using System.IO;
using Kontract.Interfaces.FileSystem.EventArgs;
using Kontract.Models.IO;
using Kore.FileSystem.Implementations;

namespace Kore.FileSystem
{
    class PhysicalFileSystemWatcher : FileSystemWatcher
    {
        private readonly PhysicalFileSystem _fileSystem;
        private readonly System.IO.FileSystemWatcher _watcher;

        public PhysicalFileSystemWatcher(PhysicalFileSystem fileSystem, UPath path):
            base(fileSystem, path)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _watcher = new System.IO.FileSystemWatcher(_fileSystem.ConvertPathToInternal(path))
            {
                Filter = "*"
            };

            _watcher.Changed += (sender, args) => RaiseChanged(Remap(args));
            _watcher.Created += (sender, args) => RaiseCreated(Remap(args));
            _watcher.Deleted += (sender, args) => RaiseDeleted(Remap(args));
            _watcher.Error += (sender, args) => RaiseError(Remap(args));
            _watcher.Renamed += (sender, args) => RaiseRenamed(Remap(args));
        }

        ~PhysicalFileSystemWatcher()
        {
            Dispose(false);
        }

        private FileChangedEventArgs Remap(FileSystemEventArgs args)
        {
            var newChangeType = args.ChangeType;
            var newPath = _fileSystem.ConvertPathFromInternal(args.FullPath);
            return new FileChangedEventArgs(FileSystem, newChangeType, newPath);
        }

        private FileSystemErrorEventArgs Remap(ErrorEventArgs args)
        {
            return new FileSystemErrorEventArgs(args.GetException());
        }

        private FileRenamedEventArgs Remap(RenamedEventArgs args)
        {
            var newChangeType = args.ChangeType;
            var newPath = _fileSystem.ConvertPathFromInternal(args.FullPath);
            var newOldPath = _fileSystem.ConvertPathFromInternal(args.OldFullPath);
            return new FileRenamedEventArgs(FileSystem, newChangeType, newPath, newOldPath);
        }
    }
}
