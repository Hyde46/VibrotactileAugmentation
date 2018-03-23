using System;
using System.IO;
using UnityEngine;

public class FileWriter {

  public static string defaultPath = Path.Combine(Directory.GetCurrentDirectory(), "defaultLog.log");

  public static string blockOrderFilePath = Path.Combine(Directory.GetCurrentDirectory(), "BlockOrder.dat");

  public static void checkFile()
  {
    Debug.Log("default path: " + FileWriter.defaultPath + "...");
    if (File.Exists(FileWriter.defaultPath))
    {
      Debug.Log("path exists...");
    }
    else
    {
      Debug.Log("create log...");
      DateTime cTime = DateTime.Now;
      using (StreamWriter outfile = new StreamWriter(new FileStream(FileWriter.defaultPath,
                                                                    FileMode.OpenOrCreate,
                                                                    FileAccess.ReadWrite,
                                                                    FileShare.None)))
      {
        outfile.Write("## sample log for leap data" + Environment.NewLine + "## created at " + cTime.ToString() + Environment.NewLine);
      }
    }
  }

  public static void initFile(string path, string info)
  {
    DateTime cTime = DateTime.Now;
    using (StreamWriter outfile = new StreamWriter(new FileStream(path,
                                                                  FileMode.OpenOrCreate,
                                                                  FileAccess.ReadWrite,
                                                                  FileShare.None)))
    {
      outfile.Write("## " + info + Environment.NewLine + "## created at " + cTime.ToString() + Environment.NewLine);
    }
  }

  public static void writeData(string toWrite)
  {
    using (StreamWriter outfile = new StreamWriter(FileWriter.defaultPath, true))
    {
      outfile.Write(toWrite + Environment.NewLine);
    }
  }

  public static void writeData(string toWrite, string path, bool append = true)
  {
    using (StreamWriter outfile = new StreamWriter(path, append))
    {
      outfile.Write(toWrite + Environment.NewLine);
    }
  }

  public static string parseAndUpdateBlockFile(string id)
  {
      string[] lines = File.ReadAllLines(FileWriter.blockOrderFilePath);
      // look for the first line that ends with none
      int tlineIndex = -1;
      int counter = 0;
      foreach (string line in lines)
      {
          if (line.EndsWith("none"))
          {
              tlineIndex = counter;
              break;
          }

          counter++;
      }

      if (tlineIndex == -1)
      {
          UnityEngine.Debug.Log("no factors available...");
          return lines[lines.Length].Split("\t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0];
      }

      string[] tokens = lines[tlineIndex].Split("\t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
      string cTime = DateTime.Now.ToString();
      tokens[1] = id;
      tokens[2] = cTime;
      string newline = tokens[0] + "\t" + tokens[1] + "\t" + tokens[2];
      lines[tlineIndex] = newline;

      // now update the file
      using (StreamWriter outfile = new StreamWriter(FileWriter.blockOrderFilePath, false))
      {
          foreach (string line in lines)
          {
              outfile.Write(line + Environment.NewLine);
          }
      }

      return tokens[0];
  }
}
