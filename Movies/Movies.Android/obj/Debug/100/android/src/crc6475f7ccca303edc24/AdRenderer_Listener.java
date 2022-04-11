package crc6475f7ccca303edc24;


public class AdRenderer_Listener
	extends com.google.android.gms.ads.AdListener
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onAdLoaded:()V:GetOnAdLoadedHandler\n" +
			"";
		mono.android.Runtime.register ("Movies.Droid.AdRenderer+Listener, Movies.Android", AdRenderer_Listener.class, __md_methods);
	}


	public AdRenderer_Listener ()
	{
		super ();
		if (getClass () == AdRenderer_Listener.class)
			mono.android.TypeManager.Activate ("Movies.Droid.AdRenderer+Listener, Movies.Android", "", this, new java.lang.Object[] {  });
	}


	public void onAdLoaded ()
	{
		n_onAdLoaded ();
	}

	private native void n_onAdLoaded ();

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
