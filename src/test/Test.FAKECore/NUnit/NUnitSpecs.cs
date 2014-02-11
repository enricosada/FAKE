using System.IO;
using Fake;
using Machine.Specifications;

namespace Test.FAKECore.NUnitSpecs
{
	public class when_running_sequential_tests
	{
		Because of = () =>
		{

		};

		It should_have_added_the_missing_files = () =>
		{
			_project.Files.ShouldContain("Git\\Merge.fs");
			_project.Files.ShouldContain("Git\\Stash.fs");
		};

	}
}