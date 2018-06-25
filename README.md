# Escc.Umbraco.Expiry

This project helps you to manage a page expiry policy for content in Umbraco.

## Read the expiry date for a content node

The expiry date for a content node is not normally available to controller code without hitting the database via the Umbraco `ContentService`. This library adds `SaveExpiryDateToExamineEventHandler` which saves a copy of the expiry date in the `ExternalIndex` in Examine, from where it can be retrieved using the following code:

	using Examine;
	using Escc.Umbraco.Expiry;

    public override ActionResult Index(RenderModel model)
    {
		var expiryDateSource = new ExpiryDateFromExamine(model.Content.Id, ExamineManager.Instance.SearchProviderCollection["ExternalSearcher"])
		var expiryDate = expiryDateSource.ExpiryDate
	} 

## Control unpublishing dates for content

The unpublish overrides configuration allows you enforce all content nodes to unpublish after 6 months as a method of enforcing content review. This is enabled when the `UnpublishOverridesSection` is present in `web.config`, even if it is blank. 

You can prevent content nodes from having an unpublish date based on either their document type alias or URL. This is useful when you want specific content for your site never to disappear, like a home page.

	<configuration>
  		<configSections>
	  		<section name="UnpublishOverridesSection" type="Escc.Umbraco.Expiry.Configuration.UnpublishOverridesSection, Escc.Umbraco.Expiry" />
   		</configSections>

		<UnpublishOverridesSection>
            <ContentTypes>
           		<add name="HomePage" level="*"/>
                <add name="HomePageItems" level="*"/>
				<add name="ContentPage" level="2"/>
            </ContentTypes>
            <Paths>
                <add name="/about-this-site/" children="*"/>
                <add name="/banners/" children=""/>
            </Paths>
		</UnpublishOverridesSection>
	</configuration>

### Prevent content from unpublishing based on its document type

You can prevent content from having an unpublish date based on its document type and level in the content tree.

Using `add name="HomePage" level="*"` would prevent a page with a document type alias of `HomePage` at any level in the content tree from having an unpublish date.

Using `add name="ContentPage" level="2"` would prevent pages with a document type alias of `ContentPage` that are at level 2 of the content tree from having an unpublish date.

The document type alias in the `name` attribute is case sensitive. The example below would configure overrides for two different document types:

	<UnpublishOverridesSection>
        <ContentTypes>
        	<add name="HomePage" level="*"/>
			<add name="homepage" level="*"/>
        </ContentTypes>
	</UnpublishOverridesSection>

### Prevent content from unpublishing based on its URL

It is also possible to prevent content from having an unpublish date based on its URL. This is useful when you have an area of a site which should never be unpublished and that has a mixture of document types, and the same document types should have an unpublish date when used elsewhere.

	<UnpublishOverridesSection>
        <Paths>
            <add name="/about-this-site/" children="*"/>
            <add name="/banners/" children=""/>
        </Paths>
	</UnpublishOverridesSection>

The `children=` attribute works similarly to the `level` attribute. If you have `children="*"` then the override also applies to all children under that path, whereas if it is left blank (eg `children=""`) then the override only applies to the single page at the specified path.

## Unpublishing dates API

By default the above policies are applied by `ExpiryRulesEventHandler` when a content node is published, which means that when you update the configuration existing content may not match the new configuration until it is next republished. An API allows you to revisit existing content and ensure it complies with the current configuration. 

You can call the API with a `POST` request to the following URL. This request must be authenticated using the authentication method configured for web APIs on the consuming site.

	https://hostname/umbraco/api/Expiry/EnsureUnpublishDatesMatchPolicy/

## Check which pages are about to be unpublished

If you are using unpublish dates as a way of forcing content to be reviewed, you will need to check what content is  coming up for review. You can do this with a `POST` request to the following URL. This request must be authenticated using the authentication method configured for web APIs on the consuming site.

	https://hostname/umbraco/api/Expiry/CheckForExpiringNodesByUser/

It expects an `inTheNextHowManyDays` parameter, and will return a list of pages due to be unpublished within a date range spanning that many days from today. The list of pages will be subdivided by the Umbraco back office user responsible, based on permissions. Where more than one user has permission to a page, the page will be listed separately for each user.

Note that in `web.config` of the target Umbraco instance you will need two settings to identify the account used to query Umbraco:

	<appSettings>
	    <add key="AdminAccountName" value="username" />
    	<add key="AdminAccountEmail" value="email.address@example.org" />
	</appSettings>

## Notify users that their pages are about to be unpublished

You can configure a scheduled task to run `Escc.Umbraco.Expiry.Notifier.exe`. This will look for pages that will expire within a set number of days, and notify the web authors responsible for those pages. The number of days is set in `app.config`.

## Email

The following configuration options can be set in `web.config` to change the text of emails sent by this application:

	<appSettings>
	    <add key="WebsiteName" value="Example Umbraco website" />
	    <add key="SiteUri" value="https://example.org/umbraco" />
	    <add key="WebAuthorsGuidanceUrl" value="https://example.org/help-pages-for-web-authors" />
	</appSettings>

You can also control when emails are sent and where to:

	<appSettings>
		<!-- When to notify web authors and the admin team about pages due to expire, with default values shown -->
		<add key="InTheNextHowManyDays" value="14" />
		<add key="EmailAdminAtDays" value="3" />
	    <add key="AdminEmail" value="website-admin@example.org" />

		<!-- Override to avoid sending emails to web authors during development -->
		<add key="ForceSendTo" value="developer-override@example.org" />
	</appSettings>

