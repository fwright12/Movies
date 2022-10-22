package crc64cc97b39ab5a7447d;


public class AndroidAppLinks_AndroidActionSuccessListener
	extends java.lang.Object
	implements
		mono.android.IGCUserPeer,
		com.google.android.gms.tasks.OnSuccessListener
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"n_onSuccess:(Ljava/lang/Object;)V:GetOnSuccess_Ljava_lang_Object_Handler:Android.Gms.Tasks.IOnSuccessListenerInvoker, Xamarin.GooglePlayServices.Tasks\n" +
			"";
		mono.android.Runtime.register ("Xamarin.Forms.Platform.Android.AppLinks.AndroidAppLinks+AndroidActionSuccessListener, Xamarin.Forms.Platform.Android.AppLinks", AndroidAppLinks_AndroidActionSuccessListener.class, __md_methods);
	}


	public AndroidAppLinks_AndroidActionSuccessListener ()
	{
		super ();
		if (getClass () == AndroidAppLinks_AndroidActionSuccessListener.class) {
			mono.android.TypeManager.Activate ("Xamarin.Forms.Platform.Android.AppLinks.AndroidAppLinks+AndroidActionSuccessListener, Xamarin.Forms.Platform.Android.AppLinks", "", this, new java.lang.Object[] {  });
		}
	}


	public void onSuccess (java.lang.Object p0)
	{
		n_onSuccess (p0);
	}

	private native void n_onSuccess (java.lang.Object p0);

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
