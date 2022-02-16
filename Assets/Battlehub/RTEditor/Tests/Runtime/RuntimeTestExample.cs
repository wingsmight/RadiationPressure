using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Tests
{
    public class RuntimeTestExample
    {
        // A Test behaves as an ordinary method
        [Test]
        public void RuntimeTestExampleSimplePasses()
        {
            Assert.Inconclusive();
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator RuntimeTestExampleWithEnumeratorPasses()
        {
            Assert.Inconclusive();
            yield return null;
        }
    }
}
