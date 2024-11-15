using System.Text;

internal class Program
{
   private static void Main( string[] args )
   {
      Encoding.RegisterProvider( CodePagesEncodingProvider.Instance );

      Console.WriteLine( "Started :)" );

      CRTDebugMacroAdder adder = new();

      foreach (var filePath in Directory.GetFiles( "C:\\git\\CamtasiaWin", "*.cpp", SearchOption.AllDirectories ))
      {
         if (filePath.Contains( "CommonCpp", StringComparison.CurrentCultureIgnoreCase ))
            continue;

         if (filePath.Contains( "stdafx", StringComparison.CurrentCultureIgnoreCase ))
            continue;
         if (filePath.Contains( @"\tests\", StringComparison.CurrentCultureIgnoreCase ))
            continue;
         if (filePath.Contains( @"\setup\", StringComparison.CurrentCultureIgnoreCase ))
            continue;
         if (filePath.Contains( "interop", StringComparison.CurrentCultureIgnoreCase ))
            continue;

         adder.EnsureHasDebugMacro( filePath );
      }

      Console.WriteLine( "Finished :)" );
   }
}