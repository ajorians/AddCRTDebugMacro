using System.Diagnostics;
using System.Text;

internal static class EncodingUtils
{
   private static int GetCharacterLength( string filename, Encoding fileencoding )
   {
      int length = int.MaxValue;
      try
      {
         var contents = File.ReadAllText( filename, fileencoding );
         length = contents.Length;
      }
      catch
      {
         // ignored
      }
      return length;
   }

   /// <summary>
   /// Determines a text file's encoding by analyzing its byte order mark (BOM).
   /// Defaults to ASCII when detection of the text file's endianness fails.
   /// </summary>
   /// <param name="filename">The text file to analyze.</param>
   /// <returns>The detected encoding.</returns>
   public static Encoding GetEncoding( string filename )
   {
      const int heuristicLength = 32768;

      FileInfo f = new FileInfo(filename);
      if (f.Length < 4)
      {
         return Encoding.ASCII;
      }

      using (var file = new FileStream( filename, FileMode.Open, FileAccess.Read ))
      {
         // Read the BOM
         var bom = new byte[4];
         file.Read( bom, 0, 4 );

         // Analyze the BOM
         if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76)
            return Encoding.UTF7;
         if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf)
            return Encoding.UTF8;
         if (bom[0] == 0xff && bom[1] == 0xfe)
            return Encoding.Unicode; //UTF-16LE
         if (bom[0] == 0xfe && bom[1] == 0xff)
            return Encoding.BigEndianUnicode; //UTF-16BE
         if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff)
            return Encoding.UTF32;

         // no BOM, let's guess
         file.Position = 0;
         byte[] sampleBytes = new byte[Math.Min( file.Length, heuristicLength)];
         file.Read( sampleBytes, 0, sampleBytes.Length );

         // let's check if it is likely to be a UTF16 kind
         int nbOdds  = 0;
         int nbEvens = 0;
         // check for nulls
         for (int i = 0; i < sampleBytes.Length; i++)
         {
            if (sampleBytes[i] == 0)
            {
               if (i % 2 == 0)
               {
                  nbEvens++;
               }
               else
               {
                  nbOdds++;
               }
            }
         }
         if (nbEvens > 0.4 * sampleBytes.Length && nbOdds < 0.1 * sampleBytes.Length)
         {
            return new UnicodeEncoding( true, false );
         }
         if (nbOdds > 0.4 * sampleBytes.Length && nbEvens < 0.1 * sampleBytes.Length)
         {
            return new UnicodeEncoding( false, false );
         }
      }

      // last resort, let's use the smallest of ASCII and UTF8
      int iAscii = GetCharacterLength(filename, Encoding.ASCII);
      int iUtf8 = GetCharacterLength(filename, new UTF8Encoding(false));

      //Usually when somebody talks about "the ANSI encoding" they mean Windows Code Page 1252 (or whatever their system uses).
      return iUtf8 < iAscii ? new UTF8Encoding( false ) : Encoding.GetEncoding( "Windows-1252" );
   }
}

public class CRTDebugMacroAdder
{
   public void EnsureHasDebugMacro( string filePath )
   {
      Encoding fileEncoding = EncodingUtils.GetEncoding( filePath );
      string fileContents = File.ReadAllText( filePath );

      if (fileContents.Contains( "DEBUG_NEW" ))
         return;

      fileContents = AddDebugMacro( fileContents );
      File.WriteAllText( filePath, fileContents, fileEncoding );
   }

   private string AddDebugMacro( string fileContents )
   {
      Debug.Assert( fileContents.Contains( "DEBUG_NEW" ) == false );

      var lines = fileContents.Split(new[] { '\n' }, StringSplitOptions.None).ToList();

      int lastIncludeIndex = lines.FindLastIndex(line => line.Contains("#include"));

      string insertMacro = """

         #ifdef _DEBUG
         #define new DEBUG_NEW
         #endif
         """;

      foreach( var lineToAdd in insertMacro.Split( new[] { "\n" }, StringSplitOptions.None ).Reverse().ToList() )
      {
         var insertedLine = lineToAdd;
         if (!insertedLine.EndsWith( '\r' ))
         {
            insertedLine += "\r";
         }
         lines.Insert( lastIncludeIndex + 1, insertedLine );
      }

      return string.Join( "\n", lines );
   }
}