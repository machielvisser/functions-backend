# Serverless email function

This Azure Function allows static websites to have an email form without the need for a backend.

It works like this:
* The website posts the following parameters:
	- appid (a guid that identifies the specific website)
	- subject
	- email (the email of the sender, so the recipient can answer)
	- message
* The functionapp then:
	- Parses the message body
	- Receives an azure table as IQueryable containing the recipients for the websites
	- Looks up the correct recipient based on the appid
	- Retrieves settings and secrets from the environment variables
	- Builds an email message and sends it
