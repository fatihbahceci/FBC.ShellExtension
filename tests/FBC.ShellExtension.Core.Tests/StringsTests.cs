using System.Reflection;
using FBC.ShellExtension.Core.Resources;

namespace FBC.ShellExtension.Core.Tests;

[TestClass]
public sealed class StringsTests
{
    [TestMethod]
    public void KnownKey_ResolvesFromTheEmbeddedResources()
    {
        Assert.AreEqual("A label is required.", Strings.ValidationLabelRequired());
        Assert.AreEqual("The maximum item count must not be less than the minimum (2).", Strings.ValidationMaxCount(2));
    }

    [TestMethod]
    public void MissingKey_FallsBackToTheKeyItself()
    {
        Assert.AreEqual("NoSuchKey", Strings.Get("NoSuchKey"));
    }

    [TestMethod]
    public void EveryAccessor_HasItsResource()
    {
        // A typo between Strings.cs and Strings.resx shows up as the bare key at run time.
        IEnumerable<MethodInfo> accessors = typeof(Strings)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.ReturnType == typeof(string) && method.Name != nameof(Strings.Get));

        foreach (MethodInfo accessor in accessors)
        {
            object[] arguments = accessor.GetParameters()
                .Select(parameter => parameter.ParameterType == typeof(string) ? "x" : (object)1)
                .ToArray();
            string text = (string)accessor.Invoke(null, arguments)!;

            Assert.AreNotEqual(accessor.Name, text, accessor.Name);
            Assert.IsFalse(string.IsNullOrWhiteSpace(text), accessor.Name);
        }
    }
}
