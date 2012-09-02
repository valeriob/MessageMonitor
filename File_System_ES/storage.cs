using CSharpTest.Net;
using CSharpTest.Net.Collections;
using CSharpTest.Net.Interfaces;
using CSharpTest.Net.IO;
using CSharpTest.Net.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace File_System_ES
{
    public class BTreeStreamStorage : INodeStorage, ITransactable
    {
        private TransactedCompoundFile _file;
        private readonly FileId _rootId;
        private readonly TransactedCompoundFile.Options _options;

        /// <summary>
        /// Opens an existing BPlusTree file at the path specified, for a new file use CreateNew()
        /// </summary>
        public BTreeStreamStorage(TransactedCompoundFile.Options options)
        {
            _options = options;
            _file = new TransactedCompoundFile(options);
            _rootId = new FileId(TransactedCompoundFile.FirstIdentity);

            if (options.CreateNew)
                CreateRoot(_file);
        }

        /// <summary>
        /// Closes the file in it's current state.
        /// </summary>
        public void Dispose()
        {
            _file.Dispose();
        }

        public void Commit()
        {
            _file.Commit();
        }

        public void Rollback()
        {
            _file.Rollback();
        }


        private static void CreateRoot(TransactedCompoundFile file)
        {
            uint rootId;
            rootId = file.Create();
            if (rootId != TransactedCompoundFile.FirstIdentity)
                throw new InvalidNodeHandleException();

            file.Write(rootId, new byte[0], 0, 0);
            file.Commit();
        }

        public void Reset()
        {
            _file.Clear();
            CreateRoot(_file);
        }

        public IStorageHandle OpenRoot(out bool isNew)
        {
            using (Stream s = _file.Read(_rootId.Id))
                isNew = s.ReadByte() == -1;
            return _rootId;
        }

        public bool TryGetNode<TNode>(IStorageHandle handleIn, out TNode node, ISerializer<TNode> serializer)
        {
            Check.Assert<InvalidNodeHandleException>(handleIn is FileId);
            FileId handle = (FileId)handleIn;
            using (Stream s = _file.Read(handle.Id))
            {
                node = serializer.ReadFrom(s);
                return true;
            }
        }

        public IStorageHandle Create()
        {
            FileId hnew = new FileId(_file.Create());
            return hnew;
        }

        public void Destroy(IStorageHandle handleIn)
        {
            Check.Assert<InvalidNodeHandleException>(handleIn is FileId);
            FileId handle = (FileId)handleIn;
            _file.Delete(handle.Id);
        }

        public void Update<T>(IStorageHandle handleIn, ISerializer<T> serializer, T node)
        {
            Check.Assert<InvalidNodeHandleException>(handleIn is FileId);
            FileId handle = (FileId)handleIn;
            using (MemoryStream s = new MemoryStream())
            {
                serializer.WriteTo(node, s);
                _file.Write(handle.Id, s.GetBuffer(), 0, (int)s.Position);
            }
        }

        void ISerializer<IStorageHandle>.WriteTo(IStorageHandle handleIn, Stream stream)
        {
            FileId fid = ((FileId)handleIn);
            PrimitiveSerializer.UInt32.WriteTo(fid.Id, stream);
            PrimitiveSerializer.UInt32.WriteTo(fid.Unique, stream);
        }

        IStorageHandle ISerializer<IStorageHandle>.ReadFrom(Stream stream)
        {
            return new FileId(
                PrimitiveSerializer.UInt32.ReadFrom(stream),
                PrimitiveSerializer.UInt32.ReadFrom(stream)
                );
        }

        [DebuggerDisplay("{Id}")]
        struct FileId : IStorageHandle
        {
            public readonly uint Id;
            public readonly uint Unique;
            public FileId(uint id)
                : this(id, (uint)new Crc32(Guid.NewGuid().ToByteArray()).Value)
            { }
            public FileId(uint id, uint unique)
            { Id = id; Unique = unique; }

            bool IEquatable<IStorageHandle>.Equals(IStorageHandle other)
            { return Equals(other); }
            public override bool Equals(object other)
            {
                if (!(other is FileId)) return false;
                FileId fid = ((FileId)other);
                return Id.Equals(fid.Id) && Unique.Equals(fid.Unique);
            }
            public override int GetHashCode()
            { return (int)(Id ^ Unique); }
        }
    }
}
