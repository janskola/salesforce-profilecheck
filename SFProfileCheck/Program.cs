using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SFProfileCheck
{
    class Program
    {
        static string MetadataDirectory = "";
        static string ProfileFile = "";

        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("SFProfileCheck <metadata_dir_location> <profile_file_name>");
                return;
            }

            MetadataDirectory = args[0];
            ProfileFile = args[1];

            CheckProfile();
        }

        static void CheckProfile()
        {
            string Extension = Path.GetExtension(ProfileFile);

            string ProfilePathName = "";
            switch (Extension)
            {
                case ".profile":
                    ProfilePathName = Path.Combine(MetadataDirectory, "profiles", ProfileFile);
                    break;
                case ".permissionset":
                    ProfilePathName = Path.Combine(MetadataDirectory, "permissionsets", ProfileFile);
                    break;
                default:
                    throw new Exception("Unknown filetype, only accepts .profile and .permissionset");
            }

            XmlDocument xProfile = new XmlDocument();
            xProfile.Load(ProfilePathName);

            XmlElement eProfile = (XmlElement)(xProfile.ChildNodes[1]);
            List<XmlElement> lProfileRemove = new List<XmlElement>();

            string ComponentName = "";
            bool remove = false;
            
            foreach (XmlElement eChild in eProfile)
            {
                remove = false;

                switch(eChild.Name)
                {
                    // all known elements listed, checking only those which refers to some other component
                    case "applicationVisibilities":
                        ComponentName = eChild.GetElementsByTagName("application").Item(0).InnerText;
                        remove = CheckForApplication(ComponentName);
                        break;
                    case "classAccesses":
                        ComponentName = eChild.GetElementsByTagName("apexClass").Item(0).InnerText;
                        remove = CheckForClass(ComponentName);
                        break;
                    case "custom":
                        break;
                    case "description":
                        break;
                    case "fieldPermissions":
                        ComponentName = eChild.GetElementsByTagName("field").Item(0).InnerText;
                        remove = CheckForField(ComponentName);
                        break;
                    case "layoutAssignments":
                        ComponentName = eChild.GetElementsByTagName("layout").Item(0).InnerText;
                        bool removeLayout = CheckForLayout(ComponentName);

                        bool removeRecordtype = false;
                        XmlNodeList xlRecordType = eChild.GetElementsByTagName("recordType");
                        if (xlRecordType.Count > 0)
                        {
                            ComponentName = xlRecordType.Item(0).InnerText;
                            removeRecordtype = CheckForRecordtype(ComponentName);
                        }

                        remove = removeLayout || removeRecordtype;
                        break;
                    case "objectPermissions":
                        ComponentName = eChild.GetElementsByTagName("object").Item(0).InnerText;
                        remove = CheckForObject(ComponentName);
                        break;
                    case "pageAccesses":
                        ComponentName = eChild.GetElementsByTagName("apexPage").Item(0).InnerText;
                        remove = CheckForPage(ComponentName);
                        break;
                    case "recordTypeVisibilities":
                        ComponentName = eChild.GetElementsByTagName("recordType").Item(0).InnerText;
                        remove = CheckForRecordtype(ComponentName);
                        break;
                    case "tabVisibilities":
                        ComponentName = eChild.GetElementsByTagName("tab").Item(0).InnerText;
                        remove = CheckForTab(ComponentName);
                        break;
                    case "tabSettings":
                        ComponentName = eChild.GetElementsByTagName("tab").Item(0).InnerText;
                        remove = CheckForTab(ComponentName);
                        break;
                    case "userLicense":
                        break;
                    case "userPermissions":
                        break;
                    case "hasActivationRequired":
                        break;
                    case "label":
                        break;
                    case "loginIpRanges":
                        break;
                    default:
                        throw new Exception("Unknown element in Profile definition: " + eChild.Name);
                }

                if (remove)
                {
                    Console.WriteLine("Removing " + eChild.Name + ": " + ComponentName);
                    lProfileRemove.Add(eChild);
                }
            }

            foreach(XmlElement eRemove in lProfileRemove)
            {
                eProfile.RemoveChild(eRemove);
            }

            using (TextWriter sw = new StreamWriter(ProfilePathName + ".new", false, Encoding.UTF8))
            {
                xProfile.Save(sw);
            }
        }

        private static bool CheckForApplication(string application)
        {
            string path = Path.Combine(MetadataDirectory, "applications", application + ".app");
            return !File.Exists(path);
        }
        private static bool CheckForClass(string apexClass)
        {
            string path = Path.Combine(MetadataDirectory, "classes", apexClass + ".cls");
            return !File.Exists(path);
        }
        private static bool CheckForField(string objectfield)
        {
            char[] separ = { '.' };
            string[] parts = objectfield.Split(separ);

            string Object = parts[0];
            string Field = parts[1];

            return CheckForObjectField(parts[0], parts[1]);
        }
        private static bool CheckForObjectField(string Object, string Field)
        {
            string path = Path.Combine(MetadataDirectory, "objects", Object + ".object");

            if (!File.Exists(path))
                return true; // not found, so remove
            else
            {
                XmlDocument xObject = new XmlDocument();
                xObject.Load(path);

                XmlElement eObject = (XmlElement)(xObject.ChildNodes[1]);

                foreach (XmlElement eChild in eObject)
                {
                    if (eChild.Name == "fields")
                    {
                        XmlNode nName = eChild.GetElementsByTagName("fullName")[0];
                        if (nName.InnerText == Field)
                            return false; // do not remove from profile
                    }
                }
                return true; // not found, so remove
            }
        }
        private static bool CheckForLayout(string layout)
        {
            string path = Path.Combine(MetadataDirectory, "layouts", layout + ".layout");
            return !File.Exists(path);
        }
        private static bool CheckForRecordtype(string objectrectype)
        {
            char[] separ = { '.' };
            string[] parts = objectrectype.Split(separ);

            string Object = parts[0];
            string Recordtype = parts[1];

            return CheckForObjectRecordtype(parts[0], parts[1]);
        }
        private static bool CheckForObjectRecordtype(string Object, string Recordtype)
        {
            string path = Path.Combine(MetadataDirectory, "objects", Object + ".object");

            if (!File.Exists(path))
                return true; // not found, so remove
            else
            {
                XmlDocument xObject = new XmlDocument();
                xObject.Load(path);

                XmlElement eObject = (XmlElement)(xObject.ChildNodes[1]);

                foreach (XmlElement eChild in eObject)
                {
                    if (eChild.Name == "recordTypes")
                    {
                        XmlNode nName = eChild.GetElementsByTagName("fullName")[0];
                        if (nName.InnerText == Recordtype)
                            return false; // do not remove from profile
                    }
                }
                return true; // not found, so remove
            }
        }
        private static bool CheckForObject(string Object)
        {
            string path = Path.Combine(MetadataDirectory, "objects", Object + ".object");
            return !File.Exists(path);
        }
        private static bool CheckForPage(string page)
        {
            string path = Path.Combine(MetadataDirectory, "pages", page + ".page");
            return !File.Exists(path);
        }
        private static bool CheckForTab(string tab)
        {
            if (tab.StartsWith("standard-"))
                return false; // is standard tab, so do not remove
            else
            {
                string path = Path.Combine(MetadataDirectory, "tabs", tab + ".tab");
                return !File.Exists(path);
            }
        }
    }
}
