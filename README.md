# Amit's Project

### Service Init

1. It seesms that the while loop runs in the UI Thread. We must find a better solution.
2. We'd like to be able to change the IP address in runtime.
	1. When changing IP in the activity, It should stop the current connection and connect to the new one.
3. Add better error handling - I think somewhere in here caused an exception and I'm not sure why.


### Better users handling
1. We need to trim the username and password when performing register or login.
2. We need to figure out what users we'd like to present in the listview.
	1. Do we want only the currently connected users?
	2. Do we want also users we talked with in the past?
	3. Note that the history is saved on the devices... in the DB.
3. We need to understand the mechanism of updating the user lists.
	1. When is it updated? In what way? Should we add or removing instances of update?
	2. What are those values used for? What values do we want to have?
	3. My current assumption is to only list connected users !


### Bugs
1. We noticed the bug of not being able to send messages when exiting the app.
	1. The connection ID is empty. Why? Where is it set?
	2. Why does it happen in only one device?
2. Does the WebRTC currently work...?


