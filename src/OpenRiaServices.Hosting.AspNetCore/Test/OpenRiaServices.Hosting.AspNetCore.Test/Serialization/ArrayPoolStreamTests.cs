using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OpenRiaServices.Hosting.AspNetCore.Serialization
{
    [TestClass]
    public class ArrayPoolStreamTests
    {
        private readonly byte[] _input;

        public ArrayPoolStreamTests()
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
            const int BufferSize = 4;
            var manager = new ArrayPoolMock();
            using (var stream = new ArrayPoolStream(manager, 1024))
            {
                stream.Reset(BufferSize);
                stream.Write(_input, 0, 2);

                var buffer = VerifyStreamContents(stream, 2, manager);
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
            const int BufferSize = 4;
            var manager = new ArrayPoolMock();
            using (var stream = new ArrayPoolStream(manager, 1024))
            {
                // Set initial capacity to same as buffer size
                stream.Reset(BufferSize);

                stream.Write(_input, 0, BufferSize);
                var buffer = VerifyStreamContents(stream, BufferSize, manager);
                Assert.AreEqual(1, manager.Allocated.Count, "Should only have allocated a single buffer");
                Assert.AreSame(manager.Allocated[0], buffer.Array, "Should reuse initial array");
            }
        }

        [TestMethod]
        public void LargeWrite()
        {
            var manager = new ArrayPoolMock();
            using (var stream = new ArrayPoolStream(manager, 1024))
            {
                // Set initial capacity of 4
                stream.Reset(4);

                stream.Write(_input, 0, 40);
                VerifyStreamContents(stream, 40, manager);
                Assert.IsTrue(manager.Allocated.Count > 2, "Multiple buffers should have been used");
            }
        }

        [TestMethod]
        public void ReuseLastBufferIfPossible()
        {
            int initalBufferSize = 4;
            int buffer2Size = initalBufferSize * 2;
            int buffer3Size = initalBufferSize + buffer2Size;
            var manager = new ArrayPoolMock();
            using (var stream = new ArrayPoolStream(manager, 1024))
            {
                stream.Reset(initalBufferSize);

                // Fill first and second buffers full
                stream.Write(_input, 0, initalBufferSize);
                stream.Write(_input, initalBufferSize, buffer2Size);
                Assert.AreEqual(buffer2Size, manager.Allocated[1].Length, "This test assumed allocation size was wrong");

                // Write next one up just a little so that everyhing should fit in the 3rd
                int streamOffsetLastBuffer = (int)stream.Position;
                stream.Write(_input, streamOffsetLastBuffer, streamOffsetLastBuffer);

                Assert.AreEqual(3, manager.Allocated.Count, "Test assumes 3 buffers");
                Assert.AreEqual((int)stream.Position, manager.Allocated[2].Length, "This test assumes allocation size is enough");

                var buffer = VerifyStreamContents(stream, streamOffsetLastBuffer * 2, manager);
                Assert.AreSame(manager.Allocated[2], buffer.Array);
            }
        }

        /// <summary>
        /// Handles a very specific case where the reused buffer scenario results in overwritten buffer.
        /// It did only trigger on x86 (not x64) and only when copying large buffers of certain sizes
        /// https://github.com/OpenRIAServices/OpenRiaServices/issues/378
        /// </summary>
        /// <remarks>
        /// We mimic the behaviour as observed under IIS x68 with 4K writes up to a size which reproduces the issue
        /// </remarks>
        [TestMethod]
        [WorkItem(738)]
        public void ReuseLastBufferIfPossible2()
        {
            const int expectedLength = 24676;
            const int BlockSize = 4096; // IIS write size
            const int numFullWrites = expectedLength / BlockSize;
            byte[] data = new byte[expectedLength];
            System.Random.Shared.NextBytes(data);

            // Use same behaviour as built in ArrayPool with round up power of 2
            var manager = new ArrayPoolMock(x => (int)BitOperations.RoundUpToPowerOf2((uint)x));
            using (var stream = new ArrayPoolStream(manager, int.MaxValue))
            {
                stream.Reset(4096);

                // We mimic the behaviour as observed under IIS x68 with 4K writes up to a size which reproduces the issue
                for (int i = 0; i < numFullWrites; ++i)
                    stream.Write(data, i * BlockSize, BlockSize);
                stream.Write(data, numFullWrites * BlockSize, (expectedLength - numFullWrites * BlockSize));

                var buffer = VerifyStreamContents(stream, expectedLength, manager, data);
                Assert.AreSame(manager.Allocated[2], buffer.Array);
            }
        }

        [TestMethod]
        public void MultipleWrites()
        {
            var manager = new ArrayPoolMock();
            using (var stream = new ArrayPoolStream(manager, 1024))
            {
                stream.Reset(4);

                stream.Write(_input, 0, 2); // Write partial buffer
                stream.Write(_input, 2, 2); // Fill next buffer, next shoul be 4 

                stream.Write(_input, 4, 2);
                stream.Write(_input, 6, manager.Allocated[1].Length); // Write past buffer into next one

                VerifyStreamContents(stream, 6 + manager.Allocated[1].Length, manager);
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

            var manager = new ArrayPoolMock(getBufferSize);

            using (var stream = new ArrayPoolStream(manager, 1024))
            {
                stream.Reset(4);

                stream.Write(_input, 0, 2); // Write some into first buffer

                shouldTrow = true;
                Assert.ThrowsExactly<InsufficientMemoryException>(() =>
                    stream.Write(_input, 2, 10)
                    );
            }

            manager.AssertEverythingIsReturned();
        }

        [TestMethod]
        public void ResetShouldAllowStreamReuse()
        {
            var manager = new ArrayPoolMock();
            using (var stream = new ArrayPoolStream(manager, 1024))
            {
                stream.Reset(4);

                stream.Write(_input, 0, 4);
                manager.Return(stream.GetRentedArrayAndClear().Array);
                manager.AssertEverythingIsReturned();

                stream.Reset(4);

                Assert.AreEqual(stream.Position, 0, "Position should be 0 after reset");

                byte[] otherInput = new byte[] { 3, 2, 1 };
                stream.Write(otherInput, 0, otherInput.Length);

                var array = stream.GetRentedArrayAndClear();
                VerifyBufferContents(array, new ArraySegment<byte>(otherInput));
                manager.Return(array.Array);
                manager.AssertEverythingIsReturned();
            }
        }

        private ArraySegment<byte> VerifyStreamContents(ArrayPoolStream stream, int count, ArrayPoolMock manager, byte[] expectedContents = null)
        {
            Assert.AreEqual(count, stream.Position, "Stream position should equal count");

            var buffer = stream.GetRentedArrayAndClear();
            VerifyBufferContents(buffer, new ArraySegment<byte>(expectedContents ?? _input, 0, count));

            // By returning buffer we also ensure that it was allocated through the buffer manager
            manager.Return(buffer.Array);
            manager.AssertEverythingIsReturned();
            return buffer;
        }

        private void VerifyBufferContents(ReadOnlySpan<byte> buffer, ReadOnlySpan<byte> expectedContents)
        {
            Assert.AreEqual(buffer.Length, expectedContents.Length, "Wrong count");
            if (buffer.SequenceEqual(expectedContents))
                return;

            for (int i = 0; i < expectedContents.Length; ++i)
            {
                byte expected = expectedContents[i];
                byte actual = buffer[i];
                if (expected != actual)
                {
                    Dump(buffer);

                    Assert.Fail($"Buffer contents is wrong, expected {expected} but buffer[{i}] = {actual}");
                }
            }
        }

        private static void Dump(ReadOnlySpan<byte> buf)
        {
            for (int j = 0; j < buf.Length; ++j)
            {
                Console.Write("{0} ", (int)buf[j]);
            }
            Console.WriteLine();
        }

        sealed class ArrayPoolMock : ArrayPool<byte>
        {
            private readonly HashSet<byte[]> _rented = new HashSet<byte[]>();
            private readonly List<byte[]> _allocated = new();
            private readonly Func<int, int> _getBufferSize;

            public IReadOnlyCollection<byte[]> Rented => _rented;
            public IReadOnlyList<byte[]> Allocated => _allocated;

            public ArrayPoolMock(Func<int, int> getBufferSize = null)
            {
                _getBufferSize = getBufferSize ?? ((int i) => i);
            }

            public override byte[] Rent(int minimumLength)
            {
                var buff = new byte[_getBufferSize(minimumLength)];
                _rented.Add(buff);
                _allocated.Add(buff);
                return buff;
            }

            public override void Return(byte[] array, bool clearArray = false)
            {
                if (!_rented.Remove(array))
                {
                    Assert.Fail("Buffer was not rented (returned twice?");
                }
            }

            public void Clear()
            {
                if (_rented.Count > 0)
                    throw new InvalidOperationException();
                _rented.Clear();
                _allocated.Clear();
            }

            public void AssertEverythingIsReturned()
            {
                Assert.AreEqual(0, _rented.Count, "Not all buffers were returned");
            }
        }
    }
}
