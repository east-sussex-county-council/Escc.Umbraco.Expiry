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

Create document types in Umbraco matching the following specification to allow editors to configure limits on the expiry dates that can be set for content.

1. Create a document type (without a template) called 'Expiry rules' with an alias of `expiryRules`. Add three properties on a tab called 'Default expiry rules':

	*	  'Allow pages to never expire' (alias `allowpagesToNeverExpire`), using the 'True/false' property editor.
	*	  'Months' (alias `months`), using a custom data type based on 'Numeric' with a minimum value of 0, step size of 1 and no maximum. Validate the property as a number.
	*	  'Days' (alias `days`), using the same custom data type as 'Months'.  Validate the property as a number.

	On the 'Permissions' tab, set the document type to be allowed at the root, and create one instance of it there. This lets you set a maximum expiry period for all content, which can be useful as a way of enforcing content review. 

	!['Expiry rules' document type](Documentation\expiry-rules.png)

2.	Create another document type (without a template) called 'Document type expiry rule' (alias `documentTypeExpiryRule`). Add five properties on a tab called 'Settings':

	* 'Document types' (alias `documentTypes`) using the 'Document type picker' property editor.
	* 'Level' (alias `level`), using a custom data type based on 'Numeric' with a minimum value of 1, step size of 1 and no maximum.
	* 'Force pages to never expire' (alias `forcePagesToNeverExpire`) using the 'True/false' property editor.
	* 'Months (alias `months`), using the same custom data type as the previous 'Months' property.  Validate the property as a number.
	* 'Days' (alias `days`), using the same custom data type as 'Months'.  Validate the property as a number.

	On the 'Permissions' tab for the 'Expiry rules' document type, allow this new document type as a child. 

	By creating an instance of this document type you can prevent content based on the document type(s) you select from having an unpublish date, or set a different limit on their unpublish date, and you can optionally limit that to a specific level in the content tree.

	!['Document type expiry rule' document type](Documentation\document-type-expiry-rule.png)

3.	Create a third document type (without a template) called 'Page expiry rule' (alias 'pageExpiryRule`). Add five properties on a tab called 'Settings':

	* 'Pages' (alias `pages`) using a custom data type based on 'Umbraco.MultiNodeTreePicker2' and configured to use Content nodes.
	* 'Apply to descendant pages' (alias `applyToDescendantPages`) using the 'True/false' property editor.
	* 'Force pages to never expire' (alias `forcePagesToNeverExpire`) using the 'True/false' property editor.
	* 'Months (alias `months`), using the same custom data type as the previous 'Months' property.  Validate the property as a number.
	* 'Days' (alias `days`), using the same custom data type as 'Months'.  Validate the property as a number.

	On the 'Permissions' tab for the 'Expiry rules' document type, allow this new document type as a child.

	Creating an instance of this document type allows you to prevent content from having an unpublish date, or set a different limit on its unpublish date, based on its position in the content tree. This is useful when you have an area of a site which should never be unpublished and that has a mixture of document types, and the same document types should follow the default (or different) expiry rules when used elsewhere.

	!['Page expiry rule' document type](Documentation\page-expiry-rule.png)

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

