using System;
using System.Collections;
using UnityEngine;
using System.Net;
using Firebase.Analytics;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AdsManager : MonoBehaviour
{
    public static AdsManager instance;
    // public List<RewardButton> allRewardButtons;
    // private RewardButton currentRewardedButton;



    private const string MaxSdkKey = "Fm-Zo4_Ie4a9JOZJ7gr_D7Td1_hNGNdW8c4wg3DJ8pTRC1mJhaJwisXksD57bkdkKhe0msKufoKsp49VfSPMJy";//Sablo
    
    [SerializeField] private string InterstitialAdUnitIdGeneric = "a6f12d14cd87b060";
    [SerializeField] private string RewardedAdUnitIdGeneric = "e909c09de7b0b067";
    [SerializeField] private string BannerAdUnitId = "";
    [SerializeField] private string AppOpenAdUnitId = "5ee58377c5734ce6";

    

    public bool isBannerShowing = false;
    public bool _isAdLoaded;
    private bool _isSdkInitialized;
    private bool _isAdRemoved;

    float lastTime = 0;
    public float rVShownTime = 0;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
        {
            Destroy(this.gameObject);
        }
        DontDestroyOnLoad(this.gameObject);
#if UNITY_ANDROID
     InterstitialAdUnitIdGeneric = "2886cded5b554789";
     RewardedAdUnitIdGeneric = "df02540ef52c1a2c";
#endif
// #if UNITY_IOS
//         AppTrackingTransparency.RegisterAppForAdNetworkAttribution();
// #endif
        
        
    }

    private MaxSdkBase.SdkConfiguration _sdkConfiguration;
    private bool gdprChecking = false;

    void Start()
    {
        MaxSdkCallbacks.OnSdkInitializedEvent += (MaxSdkBase.SdkConfiguration sdkConfiguration) =>
        {
            _sdkConfiguration = sdkConfiguration;
            if (sdkConfiguration.ConsentDialogState == MaxSdkBase.ConsentDialogState.Applies || Application.isEditor)
            {
                
                Debug.Log("CountryCode"+sdkConfiguration.CountryCode);
                // Show user consent dialog
                print("Sablo Max show gdpr dialog");
                print("its a gdpr zone");

#if UNITY_IO
                if (Preferences.Instance.Gdpr_accepted == 1)
                {
                    ShowPopUp();
                } 
                else
                {
                    Preferences.Instance.Gdpr_accepted = 1;
                    PlayerPrefs.Save();
                    ShowPopUp();
                    
                }         
#else
                if (Gdpr_accepted == 1)
                {
                    ShowPopUp();
                } 
                else
                {
                    gdprChecking = true;
                    var gdprPrefab = Resources.Load("GDPR/GDPRCanvasSmart") as GameObject;
                    Instantiate(gdprPrefab);
                    Time.timeScale = 0;
                }
#endif
            }
            else if (sdkConfiguration.ConsentDialogState == MaxSdkBase.ConsentDialogState.DoesNotApply)
            {
                // No need to show consent dialog, proceed with initialization
                print("Sablo Max gdpr concent does not apply, Loading next scene");
                Gdpr_accepted = 1;
                PlayerPrefs.Save();
                ShowPopUp();
            }
            else
            {
                // Consent dialog state is unknown. Proceed with initialization, but check if the consent
                // dialog should be shown on the next application initialization
                print("GDPR Consent dialog state is unknown, but will check later , Loading next scene");
                ShowPopUp();
            }
        };

        MaxSdkCallbacks.OnSdkInitializedEvent += (MaxSdkBase.SdkConfiguration sdkConfiguration) =>
        {
            Debug.Log("MAX SDK Initialized");
            _isSdkInitialized = true;
        };
        
        MaxSdk.SetSdkKey(MaxSdkKey);
        MaxSdk.InitializeSdk();
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        
    }

    


    public void GDPRPopupAccepted()
    {
        Time.timeScale = 1;
        Gdpr_accepted = 1;
        PlayerPrefs.Save();
        gdprChecking = false;
        ShowPopUp();
    }

  

    private void ShowPopUp()
    {
#if UNITY_IOS
        AppTrackingTransparency.OnAuthorizationRequestDone += OnAuthorizationRequestDone;

        var currentStatus = AppTrackingTransparency.TrackingAuthorizationStatus;
        Debug.Log($"Current authorization status: {currentStatus.ToString()}");
        if (currentStatus != AppTrackingTransparency.AuthorizationStatus.AUTHORIZED)
        {
            Debug.Log("Requesting authorization...");
            AppTrackingTransparency.RequestTrackingAuthorization();
        }
#endif

        if (RemoveAds == 1)
        {
            _isAdRemoved = true;
        }
        
        MaxSdk.SetHasUserConsent(true);
        MaxSdk.SetIsAgeRestrictedUser(false);
        MaxSdk.SetDoNotSell(false);
        
        InitializeInterstitialAds();
        InitializeRewardedAds();
       //InitializeAppOpenAds();
       InitializeBannerAds();
        InitializePaidEvents();
        

        Debug.Log("StatLoading");
        SceneManager.LoadScene("Scenes/singletonScene");
        
    }
#if UNITY_IOS
    private void OnAuthorizationRequestDone(AppTrackingTransparency.AuthorizationStatus status)
    {
        switch(status)
        {
            case AppTrackingTransparency.AuthorizationStatus.NOT_DETERMINED:
                Debug.Log("AuthorizationStatus: NOT_DETERMINED");
                break;
            case AppTrackingTransparency.AuthorizationStatus.RESTRICTED:
                Debug.Log("AuthorizationStatus: RESTRICTED");
                break;
            case AppTrackingTransparency.AuthorizationStatus.DENIED:
                Debug.Log("AuthorizationStatus: DENIED");
                Preferences.Instance.Interstitial_frequency = 15;

                AudienceNetwork.AdSettings.SetAdvertiserTrackingEnabled(false);

                break;
            case AppTrackingTransparency.AuthorizationStatus.AUTHORIZED:
                Debug.Log("AuthorizationStatus: AUTHORIZED");
                AudienceNetwork.AdSettings.SetAdvertiserTrackingEnabled(true);
                break;
        }
    }
#endif
    
    
    // public void CheckButtonsStatus()
    // {
    //
    //     if (_isAdLoaded)
    //     {
    //         for (var index = 0; index < allRewardButtons.Count; index++)
    //         {
    //             allRewardButtons[index].EnableMe();
    //         }
    //     }
    //     else
    //     {
    //         for (var index = 0; index < allRewardButtons.Count; index++)
    //         {
    //             allRewardButtons[index].DisableMe();
    //         }
    //     }
    // }


    #region MaxPaid Event

    void InitializePaidEvents()
    {
        Debug.Log("InitializePaidEvents");
        // Attach callbacks based on the ad format(s) you are using
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
      // MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        // MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;

    }

    private void OnAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo impressionData)
    {
        Debug.Log("OnAdRevenuePaidEvent");
        double revenue = impressionData.Revenue;
        var impressionParameters = new[] {
            new Firebase.Analytics.Parameter("ad_platform", "AppLovin"),
            new Firebase.Analytics.Parameter("ad_source", impressionData.NetworkName),
            new Firebase.Analytics.Parameter("ad_unit_name", impressionData.AdUnitIdentifier),
            new Firebase.Analytics.Parameter("ad_format", impressionData.AdFormat),
            new Firebase.Analytics.Parameter("value", revenue),
            new Firebase.Analytics.Parameter("currency", "USD"), // All AppLovin revenue is sent in USD
        };
        Firebase.Analytics.FirebaseAnalytics.LogEvent("ad_impression_max", impressionParameters);
    }

    #endregion
    
   //  #region AppOpen Ad Methods
   //  
   //  [SerializeField] private float appOpenTimeDelay = 30f;
   //  public bool isAppOpenShowing;
   //
   //
   //  // public bool IsAllowToShowAppOpen()
   //  // {
   //  //     Debug.Log("IsAllowToShowAppOpen "+(Time.time-lastTime));
   //  //     return Time.time-lastTime>7f;
   //  // }
   //
   //  public void OnAppOpenDismissed()
   //  {
   //      isAppOpenShowing = false;
   //      if (Time.time - lastTime > appOpenTimeDelay)
   //      {
   //          lastTime = Time.time-(Preferences.Instance.Interstitial_frequency-appOpenTimeDelay);
   //      }
   //  }
   //
   //  private void InitializeAppOpenAds()
   //  {
   //      // Attach callbacks
   //      MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnApOpenLoadedEvent;
   //      MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnApOpenFailedEvent;
   //      MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += ApOpenFailedToDisplayEvent;
   //      MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAppOpenDismissedEvent;
   //      MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnApOpenDisplayedEvent;
   //      // Load the first interstitial
   //      StartCoroutine(PriorityInterstitialAdLoaded(InterstitialAdUnitIdGeneric));
   //  }
   //
   //  private IEnumerator PriorityApOpenAdLoaded(string adUnit)
   //  {
   //      Debug.Log("ApOpen ad requesting to load  " + adUnit);
   //      yield return new WaitForSeconds(1f);
   //      LoadApOpen();
   //  }
   //  
   //  void LoadApOpen()
   //  {
   //      if (!MaxSdk.IsAppOpenAdReady(AppOpenAdUnitId))
   //          MaxSdk.LoadAppOpenAd(AppOpenAdUnitId);
   //  }
   //
   //
   //
   //  void ShowAppOpenAdIfReady()
   //  {
   //      if (isAppOpenShowing ||_isAdRemoved ||  isAdShowing)
   //          return;
   //      
   //      if (!_isAdRemoved)
   //      {
   //          if (RemoveAds == 1)
   //          {
   //              _isAdRemoved = true;
   //              return;
   //          }
   //      }
   //      
   //      Debug.Log("ShowingAppOpen");
   //      if (MaxSdk.IsAppOpenAdReady(AppOpenAdUnitId))
   //      {
   //          MaxSdk.ShowAppOpenAd(AppOpenAdUnitId);
   //      }
   //      else
   //      {
   //          MaxSdk.LoadAppOpenAd(AppOpenAdUnitId);
   //      }
   //  }
   //  
   //  private void OnApplicationPause(bool pauseStatus)
   //  {
   //      if (!pauseStatus)
   //      {
   //          ShowAppOpenAdIfReady();
   //      }
   //  }
   //
   //  public bool isApOpenLoaded;
   //  private void OnApOpenLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
   //  {
   //      // Interstitial ad is ready to be shown. MaxSdk.IsInterstitialReady(interstitialAdUnitId) will now return 'true'
   //      Debug.Log("ApOpen loaded with adInfo" + adInfo);
   //      isApOpenLoaded = true;
   //  }
   //
   //  private void OnApOpenFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo adInfo)
   //  {
   //      Debug.Log("ApOpen failed to load with error code: " + adInfo + " " + adUnitId);
   //     
   //      StartCoroutine(PriorityApOpenAdLoaded(AppOpenAdUnitId));
   //
   //  }
   //
   //  private void ApOpenFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo,MaxSdkBase.AdInfo adInfo)
   //  {
   //      Debug.Log("_AppOpen failed to display with error code: " + errorInfo + "  " + adInfo);
   //    
   //      StartCoroutine(PriorityApOpenAdLoaded(AppOpenAdUnitId));
   //          
   //
   //  }
   //
   //  private void OnAppOpenDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
   //  {
   //      isApOpenLoaded = false;
   //      // ApOpen ad is hidden. Pre-load the next ad
   //      Debug.Log("AppOpenDismissed " + adInfo);
   //      OnAppOpenDismissed();
   //      StartCoroutine(PriorityApOpenAdLoaded(AppOpenAdUnitId));
   //  }
   //  
   // // public string interstitialPlacement = "level_up";
   //  private void OnApOpenDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
   //  {
   //      if (AnalyticEvents.Instance)
   //      {
   //          FirebaseAnalytics.LogEvent("app_open_shown_event");
   //      }
   //
   //      isAppOpenShowing = true;
   //  }
   //
   //  #endregion

    #region Interstitial Ad Methods

    private void InitializeInterstitialAds()
    {
        // Attach callbacks
        MaxSdkCallbacks.OnInterstitialLoadedEvent += OnInterstitialLoadedEvent;
        MaxSdkCallbacks.OnInterstitialLoadFailedEvent += OnInterstitialFailedEvent;
        MaxSdkCallbacks.OnInterstitialAdFailedToDisplayEvent += InterstitialFailedToDisplayEvent;
        MaxSdkCallbacks.OnInterstitialHiddenEvent += OnInterstitialDismissedEvent;
        MaxSdkCallbacks.OnInterstitialDisplayedEvent += OnInterstitialDisplayedEvent;
        // Load the first interstitial
        StartCoroutine(PriorityInterstitialAdLoaded(InterstitialAdUnitIdGeneric));
    }

    private IEnumerator PriorityInterstitialAdLoaded(string adUnit)
    {
        Debug.Log("Interstitial ad requesting to load  " + adUnit);
        yield return new WaitForSeconds(1.5f);
        LoadInterstitial(adUnit);
    }
    void LoadInterstitial(string adUnit)
    {
        if (!MaxSdk.IsInterstitialReady(adUnit))
            MaxSdk.LoadInterstitial(adUnit);
    }
    
    
    public void ShowInterstitialWithoutConditions(string placement)
    {
        if (!_isAdRemoved)
        {
            if (RemoveAds == 1)
            {
                _isAdRemoved = true;
                return;
            }
        }
        if (MaxSdk.IsInterstitialReady(InterstitialAdUnitIdGeneric))
        {
            MaxSdk.ShowInterstitial(InterstitialAdUnitIdGeneric);
        }

    }

    
    // public void ShowInterstitialWithConditions(string placement)
    // {
    //     if (!_isAdRemoved)
    //     {
    //         if (DataManager.RemoveAds == 1)
    //         {
    //             _isAdRemoved = true;
    //             return;
    //         }
    //     }
    //
    //     if (Time.time - lastTime > Preferences.Instance.Interstitial_frequency)
    //     {
    //         //  interstitialPlacement = placement;
    //         interstitialConditions();
    //         //  Debug.Log("Int frequency true");
    //     }
    //     else
    //     {
    //         // Debug.Log("int frequency false");
    //
    //     }
    //
    // }

    // void interstitialConditions()
    // {
    //     if (Time.realtimeSinceStartup > Preferences.Instance.Interstitial_first_delay
    //         && GameManagerNew.Instance.currentLevel >= Preferences.Instance.Interstitial_first_level
    //     )
    //     {
    //         if (Time.time - rVShownTime > Preferences.Instance.Inter_pause_after_RV)
    //         {
    //             if (!_isAdRemoved)
    //             {
    //                 if (DataManager.RemoveAds == 1)
    //                 {
    //                     _isAdRemoved = true;
    //                     return;
    //                 }
    //             }
    //
    //            
    //             if (MaxSdk.IsInterstitialReady(InterstitialAdUnitIdGeneric))
    //             {
    //                 MaxSdk.ShowInterstitial(InterstitialAdUnitIdGeneric);
    //             }
    //
    //         }
    //         else
    //         {
    //            // Debug.Log("Inter after RV pause time false");
    //         }
    //
    //     }
    //     else
    //     {
    //        // Debug.Log("You dont meet server data requirement for interstitial");
    //     }
    // }




    public bool isInterstitialLoaded;
    private void OnInterstitialLoadedEvent(string adUnitId)
    {
        // Interstitial ad is ready to be shown. MaxSdk.IsInterstitialReady(interstitialAdUnitId) will now return 'true'
        Debug.Log("Interstitial loaded " + adUnitId);
        isInterstitialLoaded = true;
    }

    private void OnInterstitialFailedEvent(string adUnitId, int errorCode)
    {
        Debug.Log("_Interstitial failed to load with error code: " + errorCode + " " + adUnitId);
       
            StartCoroutine(PriorityInterstitialAdLoaded(InterstitialAdUnitIdGeneric));

    }

    private void InterstitialFailedToDisplayEvent(string adUnitId, int errorCode)
    {
        // Interstitial ad failed to display. We recommend loading the next ad
        Debug.Log("_Interstitial failed to display with error code: " + errorCode + "  " + adUnitId);
        StartCoroutine(PriorityInterstitialAdLoaded(InterstitialAdUnitIdGeneric));
            
    }

    private void OnInterstitialDismissedEvent(string adUnitId)
    {
        lastTime = Time.time;
        AudioListener.pause = false;
        CancelInvoke(nameof(DelayInterstitialShow));
        Invoke(nameof(DelayInterstitialShow),5f);
        isInterstitialLoaded = false;
        // Interstitial ad is hidden. Pre-load the next ad
        Debug.Log("_Interstitial dismissed " + adUnitId);
       
        StartCoroutine(PriorityInterstitialAdLoaded(InterstitialAdUnitIdGeneric));
      
        
    }


    void DelayInterstitialShow()
    {
        isAdShowing = false;
    }
    
    public bool isAdShowing;
    private void OnInterstitialDisplayedEvent(string adUnitId)
    {
        isAdShowing = true;
        AudioListener.pause = true;
        lastTime = Time.time;
        Debug.Log("_Interstitial Ads Is Displayed: " + adUnitId);
        FirebaseAnalytics.LogEvent("interstitial_shown_event");
        // //events
        ++Interstitial_count;

        switch (Interstitial_count)
        {
            case 1:
                FirebaseAnalytics.LogEvent("first_interstitial");
                break;
            case 5:
                FirebaseAnalytics.LogEvent("fifth_interstitial");
                break;
            case 10:
                FirebaseAnalytics.LogEvent("tenth_interstitial");
                break;
        }
    }

    #endregion

    #region Rewarded Ad Methods

    private void InitializeRewardedAds()
    {
        // Attach callbacks
        MaxSdkCallbacks.OnRewardedAdLoadedEvent += OnRewardedAdLoadedEvent;
        MaxSdkCallbacks.OnRewardedAdLoadFailedEvent += OnRewardedAdFailedEvent;
        MaxSdkCallbacks.OnRewardedAdFailedToDisplayEvent += OnRewardedAdFailedToDisplayEvent;
        MaxSdkCallbacks.OnRewardedAdDisplayedEvent += OnRewardedAdDisplayedEvent;
        MaxSdkCallbacks.OnRewardedAdClickedEvent += OnRewardedAdClickedEvent;
        MaxSdkCallbacks.OnRewardedAdHiddenEvent += OnRewardedAdDismissedEvent;
        MaxSdkCallbacks.OnRewardedAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;
        StartCoroutine(PriorityRewardedLoad(RewardedAdUnitIdGeneric));
    }

    private IEnumerator PriorityRewardedLoad(string adUnit)
    {
        yield return new WaitForSeconds(1.5f);
        LoadRewardedAd(adUnit);

    }
    private void LoadRewardedAd(string adUnit)
    {
        Debug.Log("Rewarded ad requesting to load " + adUnit);
        if (!MaxSdk.IsRewardedAdReady(adUnit))
            MaxSdk.LoadRewardedAd(adUnit);
    }

    public void ShowRewardedAd()
    {
        if (MaxSdk.IsRewardedAdReady(RewardedAdUnitIdGeneric))
        {
            isAdShowing = true;
            MaxSdk.ShowRewardedAd(RewardedAdUnitIdGeneric);
        }

        print("New Rewarded Not Ready ");
    }


    private bool _isAdFillAvailable;

    private void OnRewardedAdLoadedEvent(string adUnitId)
    {
        // Rewarded ad is ready to be shown. MaxSdk.IsRewardedAdReady(rewardedAdUnitId) will now return 'true'
        Debug.Log("Rewarded ad loaded" + " " + adUnitId);
        _isAdLoaded = true;
        // Invoke(nameof(CheckButtonsStatus), 1);
        // CheckButtonsStatus();
    }

    private void OnRewardedAdFailedEvent(string adUnitId, int errorCode)
    {
        Debug.Log("Rewarded ad failed to load with error code: " + errorCode + " " + adUnitId);
        _isAdLoaded = false;
            StartCoroutine(PriorityRewardedLoad(RewardedAdUnitIdGeneric));

       // CheckButtonsStatus();
    }

    private void OnRewardedAdFailedToDisplayEvent(string adUnitId, int errorCode)
    {
        Debug.Log("Rewarded ad failed to display with error code: " + errorCode + " " + adUnitId);
        _isAdLoaded = false;
       // CheckButtonsStatus();
        StartCoroutine(PriorityRewardedLoad(RewardedAdUnitIdGeneric));
    }

    private void OnRewardedAdDisplayedEvent(string adUnitId)
    {
        isAdShowing = true;
        AudioListener.pause = true;
        Debug.Log("Rewarded ad displayed" + adUnitId);
    }

    private void OnRewardedAdClickedEvent(string adUnitId)
    {
        Debug.Log("Rewarded ad clicked" + " " + adUnitId);
    }

    private void OnRewardedAdDismissedEvent(string adUnitId)
    {
        AudioListener.pause = false;
        Debug.Log("Rewarded ad dismissed" + " " + adUnitId);
        _isAdLoaded = false;
       // CheckButtonsStatus();
        StartCoroutine(PriorityRewardedLoad(RewardedAdUnitIdGeneric));
        CancelInvoke(nameof(DelayInterstitialShow));
        Invoke(nameof(DelayInterstitialShow),5f);
    }

    private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward)  // give reward
    {
        // Rewarded ad was displayed and user should receive the reward
        Debug.Log("Rewarded ad received reward " + " " + adUnitId);
        _isAdLoaded = false;
      //  CheckButtonsStatus();
        FirebaseAnalytics.LogEvent("ad_rewarded_shown");
        //Here give reward

        rVShownTime = Time.time;


        ++Rewarded_count;
       // print("Rewarded_count   "+Preferences.Instance.Rewarded_count);

        switch (Rewarded_count)
        {
            case 1:
                FirebaseAnalytics.LogEvent("first_rewarded");
                break;
            case 5:
                FirebaseAnalytics.LogEvent("fifth_rewarded");
                break;
            case 10:
                FirebaseAnalytics.LogEvent("tenth_rewarded");
                break;
            case 15:
                FirebaseAnalytics.LogEvent("fifteen_rewarded");
                break;
            case 20:
                FirebaseAnalytics.LogEvent("twenty_rewarded");
                break;
            case 30:
                FirebaseAnalytics.LogEvent("thirty_rewarded");
                break;
        }
    }

    #endregion

    #region Banner Ad Methods



    private void ToggleBannerVisibility()
    {
        if (!isBannerShowing)
        {
            MaxSdk.ShowBanner(BannerAdUnitId);
            isBannerShowing = true;
            //  showBannerButton.GetComponentInChildren<Text>().text = "Hide Banner";
        }
        else
        {
            MaxSdk.HideBanner(BannerAdUnitId);
            isBannerShowing = false;
            //  showBannerButton.GetComponentInChildren<Text>().text = "Show Banner";
        }
    }

    private void InitializeBannerAds()
    {
        // Banners are automatically sized to 320x50 on phones and 728x90 on tablets
        // You may use the utility method `MaxSdkUtils.isTablet()` to help with view sizing adjustments
        MaxSdk.CreateBanner(BannerAdUnitId, MaxSdkBase.BannerPosition.BottomCenter);

        // Set background or background color for banners to be fully functional
        MaxSdk.SetBannerBackgroundColor(BannerAdUnitId, Color.black);
        MaxSdkCallbacks.OnBannerAdLoadedEvent += OnBannerLoadedEvent;

    }


    public void OnBannerLoadedEvent(string BannerAdUnitId)
    {
       // AnalyticEvents.Instance.LogAd_Banner_shownEvent("game start");

    }
    public void ShowBanner()
    {
        MaxSdk.ShowBanner(BannerAdUnitId);
        isBannerShowing = true;
    }
    public void HideBanner()
    {
        MaxSdk.HideBanner(BannerAdUnitId);
        isBannerShowing = false;
    }

    #endregion

    #region SaveData

    public int Gdpr_accepted
    { 
        get => PlayerPrefs.GetInt("gdpr_accepted", 0);
        set
        {
            PlayerPrefs.SetInt("gdpr_accepted", value);
        }
    }
    
    public int RemoveAds
    { 
        get => PlayerPrefs.GetInt("remove_ads", 0);
        set
        {
            PlayerPrefs.SetInt("remove_ads", value);
        }
    }
    
    public int Interstitial_count
    { 
        get => PlayerPrefs.GetInt("Interstitial_count", 0);
        set
        {
            PlayerPrefs.SetInt("Interstitial_count", value);
        }
    }
    
    public int Rewarded_count
    { 
        get => PlayerPrefs.GetInt("Rewarded_count", 0);
        set
        {
            PlayerPrefs.SetInt("Rewarded_count", value);
        }
    }

    #endregion
    
    
    
}
