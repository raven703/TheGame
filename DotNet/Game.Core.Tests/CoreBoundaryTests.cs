using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Game.Core;
using NUnit.Framework;

public class CoreBoundaryTests
{
    [Test] public void AssemblyAndPublicSignaturesHaveNoUnityDependencies()
    {
        var assembly=typeof(ShipOrders).Assembly;
        foreach(var reference in assembly.GetReferencedAssemblies())
            Assert.False(reference.Name.StartsWith("Unity",StringComparison.Ordinal),reference.FullName);
        foreach(var type in assembly.GetExportedTypes())
        {
            Assert.False((type.Namespace??"").StartsWith("Unity",StringComparison.Ordinal));
            foreach(var member in type.GetMembers(BindingFlags.Public|BindingFlags.Instance|BindingFlags.Static))
            {
                var signature=member.ToString();Assert.False(signature.Contains("UnityEngine")||signature.Contains("UnityEditor"),signature);
            }
        }
    }
    [Test] public void SharedSourceHasNoEngineStorageClockOrHiddenRandomAccess()
    {
        var directory=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while(directory!=null&&!Directory.Exists(Path.Combine(directory.FullName,"Assets","Game.Core")))directory=directory.Parent;
        Assert.NotNull(directory,"Repository root not found");
        var core=Path.Combine(directory.FullName,"Assets","Game.Core");
        var forbidden=new Regex(@"\b(UnityEngine|UnityEditor|MonoBehaviour|ScriptableObject|Transform|GameObject|Physics2D|PlayerPrefs|JsonUtility)\b|\b(Time|Random|Guid|DateTime)\s*\.|\bSystem\.IO\b");
        foreach(var path in Directory.GetFiles(core,"*.cs",SearchOption.AllDirectories))
        {
            var text=Regex.Replace(File.ReadAllText(path),@"//[^\r\n]*|/\*[\s\S]*?\*/","");
            Assert.False(forbidden.IsMatch(text),path);
        }
        StringAssert.Contains("\"noEngineReferences\": true",File.ReadAllText(Path.Combine(core,"Game.Core.asmdef")));
        var project=File.ReadAllText(Path.Combine(directory.FullName,"DotNet","Game.Core","Game.Core.csproj"));
        StringAssert.Contains("netstandard2.0",project);StringAssert.Contains("<LangVersion>9.0</LangVersion>",project);
        StringAssert.Contains("../../Assets/Game.Core/**/*.cs",project);
        Assert.IsEmpty(Directory.GetFiles(Path.Combine(directory.FullName,"Assets"),"Game.Core.dll",SearchOption.AllDirectories));
    }
    [Test] public void StarterIdentityIsSuppliedExplicitlyAndOnlyIssuedOnce()
    {
        int calls=0;var model=new PlayerEquipment(new EquipmentSnapshot(),_=>{},()=>"identity-"+(++calls));
        var definition=new EquipmentShipDefinition {Id="one",WeaponSlots=1};
        var starter=new System.Collections.Generic.List<EquipmentItem>{new EquipmentItem("laser","template",EquipmentCategory.Weapons,WeaponMountType.Fixed,"saved")};
        Assert.True(model.EnsureShip(definition,starter,true,out _));Assert.AreEqual("identity-1",model.Snapshot.Inventory.Single().InstanceId);
        Assert.True(model.EnsureShip(definition,starter,true,out _));Assert.AreEqual(1,calls);
    }
}