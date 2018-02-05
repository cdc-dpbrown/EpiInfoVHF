using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ContactTracing.Core.Enums;
using ContactTracing.Core.Events;

namespace ContactTracing.Core
{



    /// <summary>
    ///  ** Singleton  **  
    /// This class is resp for global changes in the client application state      
    /// </summary>
    public sealed class ApplicationViewModel
    {

        #region   ** Singleton Instance  **
        static readonly ApplicationViewModel _instance = new ApplicationViewModel();
        public static ApplicationViewModel Instance
        {
            get
            {
                return _instance;
            }
        }
        #endregion


        /// <summary>
        /// Currently the region is set only when there are no projects open, and only 
        /// from one place in the UI.  
        /// 
        /// However it is a better practice to have the UI set a property on the ApplicationViewModel 
        /// and to give the responsibility  of actually setting the region to the ApplicationViewModel.  
        /// 
        /// This way if the UI changes later, the critical task of changing the region is 
        /// tucked safely away in the view model.        
        /// </summary>
        public static event RegionChangedHandler RegionChanged = delegate { };    


        public delegate void RegionChangedHandler(object sender, RegionChangedEventArgs e);


        /// <summary>
        /// This dictionary is necessary to reliably implement the anti-pattern 
        /// of “en-us”  used for non US cultures and “en” (generic English ) used for US.    
        /// </summary>
        Dictionary<string, string> cultureLanguageDictionary;
        Dictionary<string, string> fileScreenDictionary;

        public Dictionary<string, string> FileScreenDictionary
        {
            
            get { return fileScreenDictionary; }


        }

        public Dictionary<string, string> CultureLanguageDictionary
        {
            get { return cultureLanguageDictionary; }

        }

        /// <summary>
        /// Stores application's current Region    
        /// </summary>
        RegionEnum currentRegion;
        public RegionEnum CurrentRegion
        {
            get { return currentRegion; }
            set
            {
                currentRegion = value;
                RegionChanged(this, new RegionChangedEventArgs(value));
            }
        }


        // FYI  ===============================================================      
        // This code returns the current settihgs for the application    
        // Region = ApplicationViewModel.Instance.CurrentRegion.ToString();
        // Culture = ApplicationViewModel.Instance.CultureLanguageDictionary[Thread.CurrentThread.CurrentUICulture.Name.ToLower()];
        // Reg_culture = Region + " - " + Culture;
        // ======================================================================

        /// <summary>
        /// WARNING – This private constructor can never be made public.  
        /// Changing the constructor of this singleton class to public 
        /// will rip a hole in space-time, surely destroying us all. -DS                               
        /// </summary>
        private ApplicationViewModel()
        {

            Populate_CultureLanguageDictionary();
            Populate_FileScreenDictionary();

        }

        public int TestProp { get; set; }


        private void Populate_CultureLanguageDictionary()
        {
            cultureLanguageDictionary = new Dictionary<string, string>();

            //    ApplicationViewModel.Instance.TestProp = 77;

            cultureLanguageDictionary.Add("en-us", "Region: International; Language: English"); // anti-patern :(   VHF-151
            cultureLanguageDictionary.Add("en", "Region: United States; Language: English");    // anti-patern :(   VHF-151
         
            // the goal is to migrate users from "fr-FR" for international
            // datasets.  For not we must preserve backwards compat.  Newly 
            // creatd international datasets are created with the CORRECT 
            // cultural value of "fr"            
            cultureLanguageDictionary.Add("fr", "Region: International; Language: French");    //VHF-151
            cultureLanguageDictionary.Add("fr-fr", "Region: International; Language: French"); //VHF-151

        }    

        private  void  Populate_FileScreenDictionary(    )
        {
            fileScreenDictionary = new Dictionary<string, string>();

            fileScreenDictionary.Add("en-us", "International - English");   // anti-patern :( 
            fileScreenDictionary.Add("en", "United States - English");      // anti-patern :( 

            // the goal is to migrate users from "fr-FR" for international
            // datasets.  For not we must preserve backwards compat.  Newly 
            // creatd international datasets are created with the CORRECT 
            // cultural value of "fr"
            fileScreenDictionary.Add("fr", "International - French");                                                                                 
            fileScreenDictionary.Add("fr-fr", "International - French ");


        }


    }



}
