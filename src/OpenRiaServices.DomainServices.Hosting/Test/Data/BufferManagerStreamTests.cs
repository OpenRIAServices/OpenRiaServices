using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenRiaServices.DomainServices.Client.Test;
using BufferManagerStream = OpenRiaServices.DomainServices.Hosting.PoxBinaryMessageEncodingBindingElement.PoxBinaryMessageEncoder.BufferManagerStream;

namespace OpenRiaServices.DomainServices.Hosting.Test.Data
{
    [TestClass]
    public class BufferManagerStreamTests
    {
        // Buffer with bytes [1..255]
        private readonly byte[] _input;

        public BufferManagerStreamTests()
        {
            _input = new byte[255];
            for (int i = 0; i < _input.Length; ++i)
            {
                _input[i] = (byte)(i + 1);
            }
        }

        /// <summary>
        /// Only partially fill first buffer
        /// </summary>
        [TestMethod]
        public void SmallWrite()
        {
            SmallWrite(offset: 0);
        }


        /// <summary>
        /// Only partially fill first buffer but start at an offset
        /// </summary>
        [TestMethod]
        public void SmallWriteWithOffset()
        {
            SmallWrite(offset: 6);
        }

        public void SmallWrite(int offset)
        {
            var manager = new BufferManageMock();
            using (var stream = new BufferManagerStream(manager, offset, 4, 1024))
            {
                stream.Write(_input, 0, 2);

                var buffer = VerifyStreamContents(stream, offset, 2, manager);
                Assert.AreEqual(1, manager.Allocated.Count, "Should only have allocated a single buffer");
                Assert.AreSame(manager.Allocated[0], buffer.Array, "Should reuse initial array");
            }
        }

        /// <summary>
        /// Only partially fill first buffer
        /// </summary>
        [TestMethod]
        public void SmallWriteFullBuffer()
        {
            int initialOffset = 0, minBufferSize = 4;
            var manager = new BufferManageMock();
            using (var stream = new BufferManagerStream(manager, initialOffset, minBufferSize, 1024))
            {
                stream.Write(_input, 0, minBufferSize);
                var buffer = VerifyStreamContents(stream, initialOffset, minBufferSize, manager);
                Assert.AreEqual(1, manager.Allocated.Count, "Should only have allocated a single buffer");
                Assert.AreSame(manager.Allocated[0], buffer.Array, "Should reuse initial array");
            }
        }

        [TestMethod]
        public void LargeWrite()
        {
            LargeWrite(offset: 0);
        }

        [TestMethod]
        public void LargeWriteWithOffset()
        {
            LargeWrite(offset: 5);
        }

        public void LargeWrite(int offset)
        {
            var manager = new BufferManageMock();
            using (var stream = new BufferManagerStream(manager, offset, 4, 1024))
            {
                stream.Write(_input, 0, 40);
                VerifyStreamContents(stream, offset, 40, manager);
                Assert.IsTrue(manager.Allocated.Count > 2, "Multiple buffers should have been used");
            }
        }

        [TestMethod]
        public void ReuseLastBufferIfPossible()
        {
            int initialOffset = 0, initalBufferSize = 4;
            int buffer2Size = initalBufferSize * 2;
            int buffer3Size = initalBufferSize + buffer2Size;
            var manager = new BufferManageMock();
            using (var stream = new BufferManagerStream(manager, initialOffset, initalBufferSize, 1024))
            {
                // Fill first and second buffers full
                stream.Write(_input, 0, initalBufferSize);
                stream.Write(_input, initalBufferSize, buffer2Size);
                Assert.AreEqual(buffer2Size, manager.Allocated[1].Length, "This test assumed allocation size was wrong");

                // Write next one up just a little so that everyhing should fit in the 3rd
                int streamOffsetLastBuffer = (int)stream.Position;
                stream.Write(_input, streamOffsetLastBuffer, streamOffsetLastBuffer);

                Assert.AreEqual(3, manager.Allocated.Count, "Test assumes 3 buffers");
                Assert.AreEqual((int)stream.Position, manager.Allocated[2].Length, "This test assumes allocation size is enough");

                var buffer = VerifyStreamContents(stream, initialOffset, streamOffsetLastBuffer * 2, manager);
                Assert.AreSame(manager.Allocated[2], buffer.Array);
            }
        }

        [TestMethod]
        public void MultipleWrites()
        {
            int initialOffset = 0;
            var manager = new BufferManageMock();
            using (var stream = new BufferManagerStream(manager, initialOffset, 4, 1024))
            {
                stream.Write(_input, 0, 2); // Write partial buffer
                stream.Write(_input, 2, 2); // Fill next buffer, next shoul be 4 

                stream.Write(_input, 4, 2);
                stream.Write(_input, 6, manager.Allocated[1].Length); // Write past buffer into next one

                VerifyStreamContents(stream, initialOffset, 6 + manager.Allocated[1].Length, manager);
                Assert.IsTrue(manager.Allocated.Count > 2, "Multiple buffers should have been used");
            }
        }

        [TestMethod]
        public void ShouldHandleThrowingAllocationWithoutMemoryLeak()
        {
            bool shouldTrow = false;
            Func<int, int> getBufferSize = size =>
            {
                if (shouldTrow) throw new InsufficientMemoryException();
                else return size;
            };

            var manager = new BufferManageMock(getBufferSize);

            using (var stream = new BufferManagerStream(manager, 0, 4, 1024))
            {
                stream.Write(_input, 0, 2); // Write some into first buffer

                shouldTrow = true;
                ExceptionHelper.ExpectException<InsufficientMemoryException>(() =>
                    stream.Write(_input, 2, 10)
                    );
            }

            manager.AssertEverythingIsReturned();
        }

        private ArraySegment<byte> VerifyStreamContents(BufferManagerStream stream, int expectedOffset, int count, BufferManageMock manager)
        {
            Assert.AreEqual(count, stream.Position, "Stream position should equal count");

            var buffer = stream.GetArrayAndDispose();
            VeriryBufferContents(buffer, expectedOffset, count);

            // By returning buffer we also ensure that it was allocated through the buffer manager
            manager.ReturnBuffer(buffer.Array);
            manager.AssertEverythingIsReturned();
            return buffer;
        }

        private static void VeriryBufferContents(ArraySegment<byte> buffer, int expectedOffset, int count)
        {
            Assert.AreEqual(buffer.Offset, expectedOffset, "Wrong offset");
            Assert.AreEqual(buffer.Count, count, "Wrong count");

            for (int i = 0; i < count; ++i)
            {
                byte expected = (byte)(i + 1);
                byte actual = buffer.Array[buffer.Offset + i];
                if (expected != actual)
                {
                    Dump(buffer.Array, count);

                    Assert.Fail($"Buffer contents is wrong, expected {expected} but buffer[{buffer.Offset} + {i}] = {actual}");
                }
            }
        }

        private static void Dump(byte[] buf, int count)
        {
            for (int j = 0; j < count; ++j)
            {
                Console.Write("{0} ", (int)buf[j]);
            }
            Console.WriteLine();
        }

        sealed class BufferManageMock : BufferManager
        {
            private readonly HashSet<byte[]> _rented = new HashSet<byte[]>();
            private readonly List<byte[]> _allocated = new List<byte[]>();
            private readonly Func<int, int> _getBufferSize;

            public IReadOnlyCollection<byte[]> Rented => _rented;
            public IReadOnlyList<byte[]> Allocated => _allocated;

            public BufferManageMock(Func<int, int> getBufferSize = null)
            {
                _getBufferSize = getBufferSize ?? ((int i) => i);
            }

            public override void Clear()
            {
                if (_rented.Count > 0)
                    throw new InvalidOperationException();
                _rented.Clear();
                _allocated.Clear();
            }

            public override void ReturnBuffer(byte[] buffer)
            {
                if (!_rented.Remove(buffer))
                {
                    Assert.Fail("Buffer was not rented (returned twice?");
                }
            }

            public override byte[] TakeBuffer(int bufferSize)
            {
                var buff = new byte[_getBufferSize(bufferSize)];
                _rented.Add(buff);
                _allocated.Add(buff);
                return buff;
            }

            public void AssertEverythingIsReturned()
            {
                Assert.AreEqual(0, _rented.Count, "Not all buffers were returned");
            }
        }
    }
}
