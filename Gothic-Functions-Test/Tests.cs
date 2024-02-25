using Gothic_Functions;
using NUnit.Framework.Constraints;
using NUnit.Framework.Internal;

namespace Gothic_Functions_Test
{
	public class ValidCaseTest
	{
		public ValidCaseTest(string text, FunctionInfo expected)
		{
			Text = text;
			Expected = expected;
			Info = new FunctionBuilder(4).Build(text);
		}

		public override string ToString()
		{
			return Text;
		}

		public readonly FunctionInfo Info;
		public readonly FunctionInfo Expected;

		private readonly string Text;
	}

	[TestFixture]
	public class FunctionTests
	{
		[TestCaseSource(nameof(GetInvalidFunctions))]
		[Test]
		public void TestFails(string text)
		{
			Assert.Throws<ParserException>(() => new FunctionBuilder(4).Build(text));
		}

		[TestCaseSource(nameof(GetValidTests))]
		[Test]
		public void TestEquals(ValidCaseTest test)
		{
			Assert.Multiple(() =>
			{
				Assert.That(test.Info.Visibility, Is.EqualTo(test.Expected.Visibility));
				Assert.That(test.Info.CallingConvention, Is.EqualTo(test.Expected.CallingConvention));
				Assert.That(test.Info.ReturnType, Is.EqualTo(test.Expected.ReturnType));
				Assert.That(test.Info.Class, Is.EqualTo(test.Expected.Class));
				Assert.That(test.Info.Name, Is.EqualTo(test.Expected.Name));
				Assert.That(test.Info.IsStatic, Is.EqualTo(test.Expected.IsStatic));
				Assert.That(test.Info.IsVirtual, Is.EqualTo(test.Expected.IsVirtual));
				Assert.That(test.Info.IsConst, Is.EqualTo(test.Expected.IsConst));

				Assert.That(test.Info.Parameters.SequenceEqual(test.Expected.Parameters));
				Assert.That(test.Info.Address.SequenceEqual(test.Expected.Address));
			});
		}

		public static IEnumerable<ValidCaseTest> GetValidTests
		{
			get
			{
				yield return new ValidCaseTest(
					@"0x00761900 public: class oCShrinkHelper * __thiscall zCCacheData<class oCNpc const *,class oCShrinkHelper>::GetData(class oCNpc const * const &)",
					new FunctionInfo
					{
						Visibility = "public",
						CallingConvention = "__thiscall",
						ReturnType = "oCShrinkHelper*",
						Class = "zCCacheData<oCNpc const*, oCShrinkHelper>",
						Name = "GetData",
						Parameters = ["oCNpc const* const&"],
						IsStatic = false,
						IsVirtual = false,
						IsConst = false,
						Address = ["", "", "", "0x00761900"]
					}
				);

				yield return new ValidCaseTest(
					@"0x00760450 public: virtual __thiscall zCSparseArray<class oCNpc const *,class zCCacheData<class oCNpc const *,class oCNpc::TActiveInfo> >::~zCSparseArray<class oCNpc const *,class zCCacheData<class oCNpc const *,class oCNpc::TActiveInfo> >(void)",
					new FunctionInfo
					{
						Visibility = "public",
						CallingConvention = "__thiscall",
						ReturnType = "",
						Class = "zCSparseArray<oCNpc const*, zCCacheData<oCNpc const*, oCNpc::TActiveInfo>>",
						Name = "~zCSparseArray<oCNpc const*, zCCacheData<oCNpc const*, oCNpc::TActiveInfo>>",
						Parameters = [],
						IsStatic = false,
						IsVirtual = true,
						IsConst = false,
						Address = ["", "", "", "0x00760450"]
					}
				);

				yield return new ValidCaseTest(
					@"0x0076FD00 public: static void __cdecl oCWorld::operator delete(void *)",
					new FunctionInfo
					{
						Visibility = "public",
						CallingConvention = "__cdecl",
						ReturnType = "void",
						Class = "oCWorld",
						Name = "operator delete",
						Parameters = ["void*"],
						IsStatic = true,
						IsVirtual = false,
						IsConst = false,
						Address = ["", "", "", "0x0076FD00"]
					}
				);

				yield return new ValidCaseTest(
					@"0x00770750 public: class zCParticleEmitter & __thiscall zCParticleEmitter::operator=(class zCParticleEmitter const &)",
					new FunctionInfo
					{
						Visibility = "public",
						CallingConvention = "__thiscall",
						ReturnType = "zCParticleEmitter&",
						Class = "zCParticleEmitter",
						Name = "operator=",
						Parameters = ["zCParticleEmitter const&"],
						IsStatic = false,
						IsVirtual = false,
						IsConst = false,
						Address = ["", "", "", "0x00770750"]
					}
				);

				yield return new ValidCaseTest(
					@"0x00774650 private: static int __cdecl oCRtnManager::Sort_WayBoxLimit(struct oCRtnManager::TRtn_WayBoxLimit *,struct oCRtnManager::TRtn_WayBoxLimit *)",
					new FunctionInfo
					{
						Visibility = "private",
						CallingConvention = "__cdecl",
						ReturnType = "int",
						Class = "oCRtnManager",
						Name = "Sort_WayBoxLimit",
						Parameters = ["oCRtnManager::TRtn_WayBoxLimit*", "oCRtnManager::TRtn_WayBoxLimit*"],
						IsStatic = true,
						IsVirtual = false,
						IsConst = false,
						Address = ["", "", "", "0x00774650"]
					}
				);

				yield return new ValidCaseTest(
					@"0x00777360 public: void * __thiscall zCListSort<class oCRtnEntry>::`scalar deleting destructor'(unsigned int)",
					new FunctionInfo
					{
						Visibility = "public",
						CallingConvention = "__thiscall",
						ReturnType = "void*",
						Class = "zCListSort<oCRtnEntry>",
						Name = "ScalarDeletingDestructor",
						Parameters = ["unsigned int"],
						IsStatic = false,
						IsVirtual = false,
						IsConst = false,
						Address = ["", "", "", "0x00777360"]
					}
				);

				yield return new ValidCaseTest(
					@"0x004022C0 private: virtual class zCClassDef * __thiscall oCCSManager::_GetClassDef(void)const ",
					new FunctionInfo
					{
						Visibility = "private",
						CallingConvention = "__thiscall",
						ReturnType = "zCClassDef*",
						Class = "oCCSManager",
						Name = "_GetClassDef",
						Parameters = [],
						IsStatic = false,
						IsVirtual = true,
						IsConst = true,
						Address = ["", "", "", "0x004022C0"]
					}
				);

				yield return new ValidCaseTest(
					@"0x004032D0 public: unsigned int __thiscall std::basic_string<char,struct std::char_traits<char>,class std::allocator<char> >::max_size(void)const ",
					new FunctionInfo
					{
						Visibility = "public",
						CallingConvention = "__thiscall",
						ReturnType = "unsigned int",
						Class = "std::basic_string<char, std::char_traits<char>, std::allocator<char>>",
						Name = "max_size",
						Parameters = [],
						IsStatic = false,
						IsVirtual = false,
						IsConst = true,
						Address = ["", "", "", "0x004032D0"]
					}
				);
			}
		}

		public static IEnumerable<string> GetInvalidFunctions
		{
			get
			{
				yield return "0x00400288 <STRUCT IMAGE_SECTION_HEADER>";
				yield return "0x007BF28D _D3DXVec2Hermite@24";
				yield return "0x007BFA2C _D3DXMatrixTranspose@8";
				yield return "0x00656B80 [thunk]:public: virtual void * __thiscall zCTex_D3D::`vector deleting destructor'`adjustor{84}' (unsigned int)";
				yield return "0x00ABB4E0 class zCWavePool `public: static class zCWavePool & __cdecl zCWavePool::GetPool(void)'::`2'::thePool";
				yield return "0x00AB64F8 private: static class zCClassDef zCVobWaypoint::classDef";
			}
		}
	}
}
