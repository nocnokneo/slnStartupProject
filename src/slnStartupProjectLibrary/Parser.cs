﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace slnStartupProjectLibrary
{
    public class Parser
    {
        public static void SetStartupProject(string slnFilename, string projectName)
        {
            string text = null;
            int projectsStartOffset = -1;
            int projectsEndOffset = -1;
            List<Project> projects = new List<Project>();
            Encoding fileEncoding = new UTF8Encoding(false); // No Byte Order Mark (BOM)

            try
            {
                text = File.ReadAllText(slnFilename, fileEncoding);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to read sln: " + ex.Message);
            }
            try
            {
                Regex projectRegex = new Regex(@"(Project\(.*?EndProject)", RegexOptions.Singleline);
                MatchCollection projectMatches = projectRegex.Matches(text);
                if (projectMatches.Count == 0)
                    throw new Exception("Could not find any projects in the solution");
                foreach (Match projectMatch in projectMatches)
                    projects.Add(new Project(projectMatch.Value, projectMatch.Index));
                if (!projects.Any(p => p.ProjectName == projectName))
                    throw new Exception(string.Format("Could not find a project named \"{0}\" in \"{1}\"", projectName, slnFilename));
                int count = projects.Count(p => p.ProjectName == projectName);
                if (count != 1)
                    throw new Exception(string.Format("Project \"{0}\" found more than once. \"{1}\" is probably malformed", projectName, slnFilename));
                Project startupProject = projects.First(p => p.ProjectName == projectName);
                projectsStartOffset = projects[0].Offset;
                projectsEndOffset = projects[projects.Count - 1].Offset + projects[projects.Count - 1].CDATA.Length;
                projects.Remove(startupProject);
                projects.Insert(0, startupProject);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to parse sln: " + ex.Message);
            }
            try
            {
                string newSln = text.Substring(0, projectsStartOffset);
                foreach (Project project in projects)
                    newSln += project.CDATA + Environment.NewLine;
                newSln += text.Substring(projectsEndOffset, text.Length - projectsEndOffset);
                File.WriteAllText(slnFilename, newSln, fileEncoding);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to write sln: " + ex.Message);
            }
        }
    }
}
