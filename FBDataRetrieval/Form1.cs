using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Facebook;
using System.Text.RegularExpressions;

namespace FBDataRetrieval
{
    public partial class Form1 : Form
    {
        private string userAccountName = "ACCOUNT_NAME";
        private string access_token = "ACCESS_TOKEN";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Login();
            // Need to replace @ID and @secret with the real ones 
            string OAuthURL = @"https://www.facebook.com/dialog/oauth?client_id=@ID&client_secret=@secret&redirect_uri=https://www.facebook.com/connect/login_success.html&response_type=token";
            webBrowser1.Navigate(OAuthURL);
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (webBrowser1.Url.AbsoluteUri.Contains("access_token"))
            {
                // GET ACCESS TOKEN PROCESS
                string url1 = webBrowser1.Url.AbsoluteUri;      //https://www.facebook.com/connect/login_success.html#access_token=USER_ACCESS_TOKEN
                string url2 = url1.Substring(url1.IndexOf("access_token") + 13);
                access_token = url2.Substring(0, url2.IndexOf("&"));
                MessageBox.Show("access_token = " + access_token);

                FacebookClient fb = new FacebookClient(access_token);
                dynamic data = fb.Get("/me");

                if (data != null)
                    MessageBox.Show("Congrat! You, " + data.name + ", successfully got the access token!");
                else
                    MessageBox.Show("You, " + data.name + ", failed to get the access token... Please find solutions");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string OAuthURL = @"https://www.facebook.com/dialog/oauth?client_id=@id
                                &redirect_uri=https://www.facebook.com/connect/login_success.html
                                &response_type=token
                                &scope=publish_stream
                                &grant_type=client_credentials";
            webBrowser1.Navigate(OAuthURL);
        }

        private void btnGetList_Click(object sender, EventArgs e)
        {
            // Get a text from user typed
            if (UserName_or_ID.Text != "")
                userAccountName = UserName_or_ID.Text;

            FacebookClient fb = new FacebookClient(access_token);
            try
            {
                dynamic timelineList = fb.Get("/" + userAccountName + "/posts");  //since the latest

                // My app is v2.0+, created on June 2014. It cannot access to any available data (gender, locale) with v1.0.
                // It must be lovely if my app is created before April 2014, it couldn't help at the end.
                // Access_token must be promoted to upper permissions 
                // Otherwise I only can retrieve very short types of data
                // Retrieve data from user...

                int count = (int)timelineList.data.Count;   //  = 25 posts per page

                var db = new FaceBookDataCollectionEntities();
                int numPosts = 0;
                int recordsAffected = 0;
                int duplicatesFound = 0;

                while (true)
                {
                    for (int i = 0; i < count; i++)
                    {
                        // Deserialize data in the list
                        var dataOne = timelineList.data[i];

                        // Convert UserID_PageNum into PageNum only (For Post_Id in the database)
                        // Post_Id should exist!!!
                        int underline = dataOne.id.IndexOf('_');
                        long idNumber = Int64.Parse(dataOne.id.Substring(underline + 1));

                        //Check the data already recorded in the database AND
                        //Check if the post includes a message => which is very important part of data
                        //
                        //Insert data to the database [25 posts per page]
                        if (!db.FBPosts.Any(f => f.Post_Id == idNumber))
                        {
                            #region Initialize data values
                            // Message or Story
                            string msg = null;
                            if (dataOne.message != null)
                                msg = dataOne.message;
                            else if (dataOne.story != null)
                                msg = dataOne.story;

                            // User_Name, User_Id and Category
                            string u_name = null;
                            string u_id = null;
                            string u_category = null;
                            if (dataOne.from != null)
                            {
                                u_name = dataOne.from.name;
                                u_id = dataOne.from.id;
                                u_category = dataOne.from.category;
                            }

                            // Picture
                            string pic = null;
                            if (dataOne.picture != null)
                                pic = dataOne.picture;

                            // Link
                            string linkTo = null;
                            if (dataOne.link != null)
                                linkTo = dataOne.link;

                            // Created_Date
                            DateTime cr_date = default(DateTime);
                            if (dataOne.created_time != null)
                                cr_date = DateTime.Parse(dataOne.created_time);

                            // Updated_Date
                            DateTime ud_date = default(DateTime);
                            if (dataOne.updated_time != null)
                                ud_date = DateTime.Parse(dataOne.updated_time);

                            // Type
                            string post_type = null;
                            if (dataOne.type != null)
                                post_type = dataOne.type;

                            // Status type
                            string post_statusType = null;
                            if (dataOne.status_type != null)
                                post_statusType = dataOne.status_type;

                            // Shares Count
                            long sharesCount = 0;
                            if (dataOne.shares != null)
                                sharesCount = dataOne.shares.count;

                            //If the post is posted by other users in User's timeline (eg. you posted in your friend's timeline)
                            string otheruserId = null;
                            string otheruserName = null;
                            if (dataOne.story_tags != null)
                            {
                                dynamic storytagListTemp = dataOne.story_tags[0];   //Deserialize method!
                                dynamic storytagList = storytagListTemp[0];
                                otheruserId = storytagList.id;
                                otheruserName = storytagList.name;
                            }

                            // If the post contains a LINK to another webpage
                            string captionTitle = null;
                            string captionFrom = null;
                            string captionDescription = null;
                            if (dataOne.caption != null)
                                captionFrom = dataOne.caption;
                            if (dataOne.name != null)
                                captionTitle = dataOne.name;
                            if (dataOne.description != null)
                                captionDescription = dataOne.description;

                            #region Likes list [25 per page]
                            var likesList = dataOne.likes;
                            int likesCount = 0;     //Total
                            int countLikeList = 0;

                            if (likesList != null)
                            {
                                while (true)
                                {
                                    try
                                    {
                                        var likeData = likesList.data[countLikeList];

                                        #region Save the like in the database
                                        string likeId = likeData.id;
                                        string likeName = likeData.name;
                                        dynamic likeList = fb.Get("https://graph.facebook.com/" + likeId);
                                        countLikeList++;
                                        likesCount++;

                                        if (likeList != null && u_id != null && u_name != null)
                                        {
                                            string idstr = null;
                                            if (likeList.id != null)
                                                idstr = likeList.id;
                                            string firstN = null;
                                            if (likeList.first_name != null)
                                                firstN = likeList.first_name;
                                            string lastN = null;
                                            if (likeList.last_name != null)
                                                lastN = likeList.last_name;
                                            string link = null;
                                            if (likeList.link != null)
                                                link = likeList.link;
                                            DateTime? updatedTime = default(DateTime?);
                                            if (likeList.updated_time != null)
                                                updatedTime = Convert.ToDateTime(likeList.updated_time);

                                            var alreadyin = (from t in db.FBPostLikes
                                                             where
                                                                t.Post_Id == idNumber
                                                                && t.Id == idstr
                                                             orderby t.Post_Id
                                                             select t.Id).FirstOrDefault();

                                            if (alreadyin == null || alreadyin == "")
                                            {
                                                try
                                                {
                                                    db.FBPostLikes.AddObject(new FBPostLike
                                                    {
                                                        Post_Id = idNumber,
                                                        Id = idstr,
                                                        From_User_ID = u_id,
                                                        From_User_Name = u_name,
                                                        FirstName = firstN,
                                                        LastName = lastN,
                                                        Link = link,
                                                        FullName = likeName,
                                                        User_Updated_Time = updatedTime
                                                    });
                                                    db.SaveChanges();
                                                }
                                                catch
                                                {
                                                    //listTimeLine.Items.Add("Update Exception occurred in PostLike with Id: " + idstr + " Post_Id: " + idNumber);
                                                    db = new FaceBookDataCollectionEntities();
                                                    continue;
                                                }
                                            }
                                        }
                                        #endregion

                                        if (countLikeList == 25)
                                        {
                                            //string nextLikePageLink = likesList.paging.cursors.after;
                                            string linkToNext = likesList.paging.next;
                                            if (linkToNext == null) break;
                                            else
                                            {
                                                //var postId = dataOne.id;
                                                try
                                                {
                                                    likesList = fb.Get(linkToNext);
                                                    countLikeList = 0;
                                                    continue;
                                                }
                                                catch (FacebookOAuthException)
                                                {
                                                    TimeSpan _thirtySeconds = new TimeSpan(0, 0, 30);
                                                    System.Threading.Thread.Sleep(_thirtySeconds);
                                                    listTimeLine.Items.Add("While retrieving likes, took 30 sec break...");
                                                    //likesList = fb.Get(linkToNext);
                                                    //countLikeList = 0;
                                                    continue;
                                                }
                                            }
                                        }
                                    }
                                    catch (ArgumentException)
                                    {
                                        //MessageBox.Show("Like Count total is: " + likesCount);
                                        break;
                                    }
                                    catch (Exception)
                                    {
                                        TimeSpan _thirtySeconds = new TimeSpan(0, 0, 30);
                                        System.Threading.Thread.Sleep(_thirtySeconds);
                                        listTimeLine.Items.Add("While retrieving likes, took 30 sec break...");
                                        continue;
                                    }
                                }
                            }
                            #endregion

                            #region Comments List [25 per page]
                            var commentsList = dataOne.comments;
                            int commentsCount = 0;  //Total
                            int countCommentList = 0;

                            if (commentsList != null)
                            {
                                while (true)
                                {
                                    try
                                    {
                                        var commentData = commentsList.data[countCommentList];
                                        countCommentList++;
                                        commentsCount++;

                                        #region Save the comment in the database
                                        int underscore_comment = commentData.id.IndexOf('_');
                                        long commentId = Int64.Parse(commentData.id.Substring(underscore_comment + 1));

                                        if (u_id != null && u_name != null)
                                        {
                                            string recipientId = u_id;
                                            string recipientName = u_name;
                                            string writerId = null;
                                            if (commentData.from.id != null)
                                                writerId = commentData.from.id;
                                            string writerName = null;
                                            if (commentData.from.name != null)
                                                writerName = commentData.from.name;
                                            string c_message = null;
                                            if (commentData.message != null)
                                                c_message = commentData.message;
                                            long messageTagCount = 0;
                                            if (commentData.message_tags != null)
                                            {
                                                messageTagCount = commentData.message_tags.Count;
                                            }
                                            DateTime? createdDate = default(DateTime?);
                                            if (commentData.created_time != null)
                                                createdDate = Convert.ToDateTime(commentData.created_time);
                                            long likeCount = 0;
                                            if (commentData.like_count != null)
                                                likeCount = commentData.like_count;
                                            bool userLikes = false;
                                            if (commentData.user_likes != null)
                                                userLikes = true;

                                            var alreadyin = (from t in db.FBPostComments
                                                             where
                                                                t.Post_Id == idNumber
                                                                && t.Comment_Id == commentId
                                                                && t.Writer_ID == writerId
                                                             orderby t.Post_Id
                                                             select t.Comment_Id).FirstOrDefault();

                                            if (alreadyin == 0)
                                            {
                                                try
                                                {

                                                    db.FBPostComments.AddObject(new FBPostComment
                                                    {
                                                        Comment_Id = commentId,
                                                        Writer_ID = writerId,
                                                        Writer_Name = writerName,
                                                        Post_Id = idNumber,
                                                        Recipient_Id = recipientId,
                                                        Recipient_Name = recipientName,
                                                        Message = c_message,
                                                        Message_Tags_Count = messageTagCount,
                                                        Created_Time = createdDate,
                                                        Like_Count = likeCount,
                                                        User_Likes = userLikes
                                                    });
                                                    db.SaveChanges();
                                                }
                                                catch (UpdateException)
                                                {
                                                    //listTimeLine.Items.Add("Update Exception occurred in PostComment with WriterName: "+ writerName + " CommentID: " + commentId);
                                                    db = new FaceBookDataCollectionEntities();
                                                    continue;
                                                }
                                            }
                                        }
                                        #endregion

                                        if (countCommentList == 25)
                                        {
                                            //string nextCommentPageLink = commentsList.paging.cursors.after;
                                            string linkToNext = commentsList.paging.next;
                                            if (linkToNext == null) break;
                                            else
                                            {
                                                try
                                                {
                                                    commentsList = fb.Get(linkToNext);
                                                    countCommentList = 0;
                                                    continue;
                                                }
                                                catch (FacebookOAuthException)
                                                {
                                                    TimeSpan _thirtySeconds = new TimeSpan(0, 0, 30);
                                                    System.Threading.Thread.Sleep(_thirtySeconds);
                                                    listTimeLine.Items.Add("While retrieving comments, took 30 sec break...");
                                                    //commentsList = fb.Get(linkToNext);
                                                    //countCommentList = 0;
                                                    continue;
                                                }
                                            }
                                        }
                                    }
                                    catch (ArgumentException)
                                    {
                                        //MessageBox.Show("Comment Count total is: " + commentsList);
                                        break;
                                    }
                                }
                            }
                            #endregion
                            #endregion

                            db.FBPosts.AddObject(new FBPost
                            {
                                Post_Id = idNumber,
                                User_Name = u_name,
                                User_Id = u_id,
                                Category = u_category,
                                Message = msg,
                                Picture = pic,
                                Link = linkTo,
                                Created_Time = cr_date,
                                Updated_Time = ud_date,
                                Type = post_type,
                                Status_Type = post_statusType,
                                Shares = sharesCount,
                                Likes = likesCount,
                                Comments = commentsCount,
                                PostByOther_Id = otheruserId,
                                PostByOther_Name = otheruserName,
                                Caption = captionFrom,
                                Caption_Title = captionTitle,
                                Cap_Description = captionDescription
                            });

                            try
                            {
                                recordsAffected += db.SaveChanges();
                                numPosts++;
                            }
                            catch (UpdateException)
                            {
                                duplicatesFound++;
                            }
                        }
                        else
                        {   /* Update existing posts only which are saved a month ago */
                            var existing = (from u in db.FBPosts where u.Post_Id == idNumber select u).FirstOrDefault();
                            if (existing != null)
                            {
                                DateTime thisDay = DateTime.Today;
                                DateTime updated_Time = existing.Updated_Time.GetValueOrDefault(DateTime.Now);
                                var diffHours = (thisDay - updated_Time).TotalHours;
                                if (diffHours < 1464)
                                {
                                    #region Initialize data values
                                    // Message or Story
                                    string msg = null;
                                    if (dataOne.message != null)
                                        msg = dataOne.message;
                                    else if (dataOne.story != null)
                                        msg = dataOne.story;

                                    // User_Name, User_Id and Category
                                    string u_name = null;
                                    string u_id = null;
                                    string u_category = null;
                                    if (dataOne.from != null)
                                    {
                                        u_name = dataOne.from.name;
                                        u_id = dataOne.from.id;
                                        u_category = dataOne.from.category;
                                    }

                                    // Picture
                                    string pic = null;
                                    if (dataOne.picture != null)
                                        pic = dataOne.picture;

                                    // Link
                                    string linkTo = null;
                                    if (dataOne.link != null)
                                        linkTo = dataOne.link;

                                    // Created_Date
                                    DateTime cr_date = default(DateTime);
                                    if (dataOne.created_time != null)
                                        cr_date = DateTime.Parse(dataOne.created_time);

                                    // Updated_Date
                                    DateTime ud_date = default(DateTime);
                                    if (dataOne.updated_time != null)
                                        ud_date = DateTime.Parse(dataOne.updated_time);

                                    // Type
                                    string post_type = null;
                                    if (dataOne.type != null)
                                        post_type = dataOne.type;

                                    // Status type
                                    string post_statusType = null;
                                    if (dataOne.status_type != null)
                                        post_statusType = dataOne.status_type;

                                    // Shares Count
                                    long sharesCount = 0;
                                    if (dataOne.shares != null)
                                        sharesCount = dataOne.shares.count;

                                    //If the post is posted by other users in User's timeline (eg. you posted in your friend's timeline)
                                    string otheruserId = null;
                                    string otheruserName = null;
                                    if (dataOne.story_tags != null)
                                    {
                                        dynamic storytagListTemp = dataOne.story_tags[0];   //Deserialize method!
                                        dynamic storytagList = storytagListTemp[0];
                                        otheruserId = storytagList.id;
                                        otheruserName = storytagList.name;
                                    }

                                    // If the post contains a LINK to another webpage
                                    string captionTitle = null;
                                    string captionFrom = null;
                                    string captionDescription = null;
                                    if (dataOne.caption != null)
                                        captionFrom = dataOne.caption;
                                    if (dataOne.name != null)
                                        captionTitle = dataOne.name;
                                    if (dataOne.description != null)
                                        captionDescription = dataOne.description;

                                    #region Likes list [25 per page]
                                    var likesList = dataOne.likes;
                                    int likesCount = 0;     //Total
                                    int countLikeList = 0;

                                    if (likesList != null)
                                    {
                                        while (true)
                                        {
                                            try
                                            {
                                                var likeData = likesList.data[countLikeList];

                                                #region Save the like in the database
                                                string likeId = likeData.id;
                                                string likeName = likeData.name;
                                                dynamic likeList = fb.Get("https://graph.facebook.com/" + likeId);
                                                countLikeList++;
                                                likesCount++;

                                                if (likeList != null && u_id != null && u_name != null)
                                                {
                                                    string idstr = null;
                                                    if (likeList.id != null)
                                                        idstr = likeList.id;
                                                    string firstN = null;
                                                    if (likeList.first_name != null)
                                                        firstN = likeList.first_name;
                                                    string lastN = null;
                                                    if (likeList.last_name != null)
                                                        lastN = likeList.last_name;
                                                    string link = null;
                                                    if (likeList.link != null)
                                                        link = likeList.link;
                                                    DateTime? updatedTime = default(DateTime?);
                                                    if (likeList.updated_time != null)
                                                        updatedTime = Convert.ToDateTime(likeList.updated_time);

                                                    var alreadyin = (from t in db.FBPostLikes
                                                                     where
                                                                        t.Post_Id == idNumber
                                                                        && t.Id == idstr
                                                                     orderby t.Post_Id
                                                                     select t.Id).FirstOrDefault();

                                                    if (alreadyin == null || alreadyin == "")
                                                    {
                                                        try
                                                        {
                                                            db.FBPostLikes.AddObject(new FBPostLike
                                                            {
                                                                Post_Id = idNumber,
                                                                Id = idstr,
                                                                From_User_ID = u_id,
                                                                From_User_Name = u_name,
                                                                FirstName = firstN,
                                                                LastName = lastN,
                                                                Link = link,
                                                                FullName = likeName,
                                                                User_Updated_Time = updatedTime
                                                            });
                                                            db.SaveChanges();
                                                        }
                                                        catch
                                                        {
                                                            //listTimeLine.Items.Add("Update Exception occurred in PostLike with Id: " + idstr + " Post_Id: " + idNumber);
                                                            db = new FaceBookDataCollectionEntities();
                                                            continue;
                                                        }
                                                    }
                                                }
                                                #endregion

                                                if (countLikeList == 25)
                                                {
                                                    //string nextLikePageLink = likesList.paging.cursors.after;
                                                    string linkToNext = likesList.paging.next;
                                                    if (linkToNext == null) break;
                                                    else
                                                    {
                                                        //var postId = dataOne.id;
                                                        try
                                                        {
                                                            likesList = fb.Get(linkToNext);
                                                            countLikeList = 0;
                                                            continue;
                                                        }
                                                        catch (FacebookOAuthException)
                                                        {
                                                            TimeSpan _thirtySeconds = new TimeSpan(0, 0, 30);
                                                            System.Threading.Thread.Sleep(_thirtySeconds);
                                                            listTimeLine.Items.Add("While retrieving likes, took 30 sec break...");
                                                            //likesList = fb.Get(linkToNext);
                                                            //countLikeList = 0;
                                                            continue;
                                                        }
                                                    }
                                                }
                                            }
                                            catch (ArgumentException)
                                            {
                                                //MessageBox.Show("Like Count total is: " + likesCount);
                                                break;
                                            }
                                            catch (Exception)
                                            {
                                                TimeSpan _thirtySeconds = new TimeSpan(0, 0, 30);
                                                System.Threading.Thread.Sleep(_thirtySeconds);
                                                listTimeLine.Items.Add("While retrieving likes, took 30 sec break...");
                                                continue;
                                            }
                                        }
                                    }
                                    #endregion

                                    #region Comments List [25 per page]
                                    var commentsList = dataOne.comments;
                                    int commentsCount = 0;  //Total
                                    int countCommentList = 0;

                                    if (commentsList != null)
                                    {
                                        while (true)
                                        {
                                            try
                                            {
                                                var commentData = commentsList.data[countCommentList];
                                                countCommentList++;
                                                commentsCount++;

                                                #region Save the comment in the database
                                                int underscore_comment = commentData.id.IndexOf('_');
                                                long commentId = Int64.Parse(commentData.id.Substring(underscore_comment + 1));

                                                if (u_id != null && u_name != null)
                                                {
                                                    string recipientId = u_id;
                                                    string recipientName = u_name;
                                                    string writerId = null;
                                                    if (commentData.from.id != null)
                                                        writerId = commentData.from.id;
                                                    string writerName = null;
                                                    if (commentData.from.name != null)
                                                        writerName = commentData.from.name;
                                                    string c_message = null;
                                                    if (commentData.message != null)
                                                        c_message = commentData.message;
                                                    long messageTagCount = 0;
                                                    if (commentData.message_tags != null)
                                                    {
                                                        messageTagCount = commentData.message_tags.Count;
                                                    }
                                                    DateTime? createdDate = default(DateTime?);
                                                    if (commentData.created_time != null)
                                                        createdDate = Convert.ToDateTime(commentData.created_time);
                                                    long likeCount = 0;
                                                    if (commentData.like_count != null)
                                                        likeCount = commentData.like_count;
                                                    bool userLikes = false;
                                                    if (commentData.user_likes != null)
                                                        userLikes = true;

                                                    var alreadyin = (from t in db.FBPostComments
                                                                     where
                                                                        t.Post_Id == idNumber
                                                                        && t.Comment_Id == commentId
                                                                        && t.Writer_ID == writerId
                                                                     orderby t.Post_Id
                                                                     select t.Comment_Id).FirstOrDefault();

                                                    if (alreadyin == 0)
                                                    {
                                                        try
                                                        {

                                                            db.FBPostComments.AddObject(new FBPostComment
                                                            {
                                                                Comment_Id = commentId,
                                                                Writer_ID = writerId,
                                                                Writer_Name = writerName,
                                                                Post_Id = idNumber,
                                                                Recipient_Id = recipientId,
                                                                Recipient_Name = recipientName,
                                                                Message = c_message,
                                                                Message_Tags_Count = messageTagCount,
                                                                Created_Time = createdDate,
                                                                Like_Count = likeCount,
                                                                User_Likes = userLikes
                                                            });
                                                            db.SaveChanges();
                                                        }
                                                        catch (UpdateException)
                                                        {
                                                            //listTimeLine.Items.Add("Update Exception occurred in PostComment with WriterName: "+ writerName + " CommentID: " + commentId);
                                                            db = new FaceBookDataCollectionEntities();
                                                            continue;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        try
                                                        {
                                                            /* for existing comments */
                                                            var comment_exists = (from u in db.FBPostComments where u.Comment_Id == commentId select u).FirstOrDefault();
                                                            if (comment_exists != null)
                                                            {
                                                                DateTime created_time_comment = existing.Updated_Time.GetValueOrDefault(DateTime.Now);
                                                                var diffHours_comment = (thisDay - created_time_comment).TotalHours;
                                                                if (diffHours_comment < 1464)
                                                                {

                                                                    if (comment_exists.Message != c_message)
                                                                        comment_exists.Message = c_message;
                                                                    if (comment_exists.Message_Tags_Count != messageTagCount)
                                                                        comment_exists.Message_Tags_Count = messageTagCount;
                                                                    if (comment_exists.Like_Count != likeCount)
                                                                        comment_exists.Like_Count = likeCount;
                                                                    if (comment_exists.User_Likes != userLikes)
                                                                        comment_exists.User_Likes = userLikes;

                                                                    db.SaveChanges();
                                                                }
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            MessageBox.Show("Error occurred in Posts - Comments: " + ex);
                                                        }
                                                    }
                                                }
                                                #endregion

                                                if (countCommentList == 25)
                                                {
                                                    //string nextCommentPageLink = commentsList.paging.cursors.after;
                                                    string linkToNext = commentsList.paging.next;
                                                    if (linkToNext == null) break;
                                                    else
                                                    {
                                                        try
                                                        {
                                                            commentsList = fb.Get(linkToNext);
                                                            countCommentList = 0;
                                                            continue;
                                                        }
                                                        catch (FacebookOAuthException)
                                                        {
                                                            TimeSpan _thirtySeconds = new TimeSpan(0, 0, 30);
                                                            System.Threading.Thread.Sleep(_thirtySeconds);
                                                            listTimeLine.Items.Add("While retrieving comments, took 30 sec break...");
                                                            //commentsList = fb.Get(linkToNext);
                                                            //countCommentList = 0;
                                                            continue;
                                                        }
                                                    }
                                                }
                                            }
                                            catch (ArgumentException)
                                            {
                                                //MessageBox.Show("Comment Count total is: " + commentsList);
                                                break;
                                            }
                                        }
                                    }
                                    #endregion
                                    #endregion

                                    if (existing.Category != u_category)
                                        existing.Category = u_category;
                                    if (existing.Message != msg)
                                        existing.Message = msg;
                                    if (existing.Picture != pic)
                                        existing.Picture = pic;
                                    if (existing.Link != linkTo)
                                        existing.Link = linkTo;
                                    if (existing.Updated_Time != ud_date)
                                        existing.Updated_Time = ud_date;
                                    if (existing.Type != post_type)
                                        existing.Type = post_type;
                                    if (existing.Status_Type != post_statusType)
                                        existing.Status_Type = post_statusType;
                                    if (existing.Shares != sharesCount)
                                        existing.Shares = sharesCount;
                                    if (existing.Likes != likesCount)
                                        existing.Likes = likesCount;
                                    if (existing.Comments != commentsCount)
                                        existing.Comments = commentsCount;
                                    if (existing.Caption != captionFrom)
                                        existing.Caption = captionFrom;
                                    if (existing.Caption_Title != captionTitle)
                                        existing.Caption_Title = captionTitle;
                                    if (existing.Cap_Description != captionDescription)
                                        existing.Cap_Description = captionDescription;

                                    db.SaveChanges();
                                }
                            }
                            duplicatesFound++;
                        }
                    }

                    listTimeLine.Items.Add("Saved " + numPosts + " posts so far to the database (" + recordsAffected + " entities total); " + duplicatesFound + " duplicates not saved");
                    

                    // Continue to next page
                    if (timelineList.paging != null)
                    {
                        // GET NEXT PAGE DATA
                        string nextPageLink = timelineList.paging.next;
                        int uniqueSymbol = nextPageLink.LastIndexOf("&");
                        if (uniqueSymbol != -1)
                        {
                            nextPageLink = nextPageLink.Substring(0, uniqueSymbol);
                            int equalSymbol = nextPageLink.LastIndexOf("=");
                            if (equalSymbol != -1)
                            {
                                string nextPageNum = nextPageLink.Substring(equalSymbol + 1);

                                timelineList = fb.Get("/" + userAccountName + "/posts?until=" + nextPageNum);

                                // GET PREVIOUS PAGE DATA
                                //string prevPageLink = timelineList.paging.previous;
                                //int uniqueSymbol = prevPageLink.LastIndexOf("&");
                                //string temp = prevPageLink.Substring(uniqueSymbol - 10, 10);
                                //long prevPageNum = long.Parse(prevPageLink.Substring(uniqueSymbol - 10, 10));
                                //timelineList = fb.Get("/"+userAccountName+"/posts?since=" + prevPageNum);

                                count = (int)timelineList.data.Count;
                            }
                        }
                        if (count <= 0)
                        {
                            MessageBox.Show("No more timeline data available");
                            break;
                        }
                        else continue;
                    }
                }
            }
            catch (FacebookOAuthException ex)
            {
                listTimeLine.Items.Add("Error: " + ex);
            }
        }

        private void usersInfo_Click(object sender, EventArgs e)
        {
            // Get a text from user typed
            if (UserName_or_ID.Text != "")
                userAccountName = UserName_or_ID.Text;

            FacebookClient fb = new FacebookClient(access_token);
            try
            {
                dynamic userInfoList = fb.Get("/" + userAccountName);


                var db = new FaceBookDataCollectionEntities();
                var user = (from u in db.FBUsers where u.UserName == userAccountName select u).FirstOrDefault();
                if (user == null)
                {
                    #region Data

                    long id = 0;
                    if (userInfoList.id != null)
                        id = long.Parse(userInfoList.id);
                    string about = null;
                    if (userInfoList.about != null)
                        about = userInfoList.about;
                    bool canPost = false;
                    if (userInfoList.can_post != null)
                        canPost = userInfoList.can_post;
                    string category = null;
                    if (userInfoList.category != null)
                        category = userInfoList.category;
                    long checkins = 0;
                    if (userInfoList.checkins != null)
                        checkins = userInfoList.checkins;
                    string description = null;
                    if (userInfoList.description != null)
                        description = userInfoList.description;
                    string founded = null;
                    if (userInfoList.founded != null)
                        founded = userInfoList.founded;
                    bool hasAddedApp = false;
                    if (userInfoList.has_added_app != null)
                        hasAddedApp = userInfoList.has_added_app;
                    bool isComPage = false;
                    if (userInfoList.is_community_page != null)
                        isComPage = userInfoList.is_community_page;
                    bool isPublished = false;
                    if (userInfoList.is_published != null)
                        isPublished = userInfoList.is_published;
                    long likes = 0;
                    if (userInfoList.likes != null)
                        likes = userInfoList.likes;
                    string link = null;
                    if (userInfoList.link != null)
                        link = userInfoList.link;
                    string location = null;
                    if (userInfoList.location != null)
                    {
                        if (userInfoList.location.zip != null)
                            location = userInfoList.location.zip;
                    }
                    string name = null;
                    if (userInfoList.name != null)
                        name = userInfoList.name;
                    long talkAboutCount = 0;
                    if (userInfoList.talking_about_count != null)
                        talkAboutCount = userInfoList.talking_about_count;
                    string username = null;
                    if (userInfoList.username != null)
                        username = userInfoList.username;
                    string website = null;
                    if (userInfoList.website != null)
                        website = userInfoList.website;
                    long wereHereCount = 0;
                    if (userInfoList.were_here_count != null)
                        wereHereCount = userInfoList.were_here_count;

                    #endregion

                    db.FBUsers.AddObject(new FBUser
                    {
                        Id = id,
                        About = about,
                        Can_Post = canPost,
                        Category = category,
                        Check_Ins = checkins,
                        Description = description,
                        Founded = founded,
                        Has_Added_App = hasAddedApp,
                        Is_Community_Page = isComPage,
                        Is_Published = isPublished,
                        Likes = likes,
                        Link = link,
                        Location = location,
                        Name = name,
                        UserName = username,
                        Talking_About_Count = talkAboutCount,
                        Website = website,
                        Were_Here_Count = wereHereCount
                    });
                    db.SaveChanges();
                    listTimeLine.Items.Add("User '" + userAccountName + "' is saved in the database!");
                }
                else
                {
                    if (user.Description != userInfoList.description)
                        user.Description = userInfoList.description;
                    if (user.About != userInfoList.about)
                        user.About = userInfoList.about;
                    if (user.Website != userInfoList.website)
                        user.Website = userInfoList.website;
                    if (user.Likes != userInfoList.likes)
                        user.Likes = userInfoList.likes;
                    if (user.Talking_About_Count != userInfoList.talking_about_count)
                        user.Talking_About_Count = userInfoList.talking_about_count;
                    if (user.Check_Ins != userInfoList.checkins)
                        user.Check_Ins = userInfoList.checkins;
                    if (user.Can_Post != userInfoList.can_post)
                        user.Can_Post = userInfoList.can_post;
                    if (user.Category != userInfoList.category)
                        user.Category = userInfoList.category;

                    db.SaveChanges();

                    listTimeLine.Items.Add("Updated Successfully!! User '" + userAccountName + "' already exists in the database");
                }
            }
            catch (FacebookOAuthException ex)
            {
                listTimeLine.Items.Add("Error: " + ex);
            }
        }

        private void DirectMessage_Click(object sender, EventArgs e)
        {
            // Get a text from user typed
            if (UserName_or_ID.Text != "")
                userAccountName = UserName_or_ID.Text;

            FacebookClient fb = new FacebookClient(access_token);
            try
            {
                dynamic directMsgList = fb.Get("/" + userAccountName + "/feed");  //since the latest

                int count = (int)directMsgList.data.Count;   //  = 25 posts per page

                var db = new FaceBookDataCollectionEntities();
                int numPosts = 0;
                int recordsAffected = 0;
                int duplicatesFound = 0;

                while (true)
                {
                    for (int i = 0; i < count; i++)
                    {
                        // Deserialize data in the list
                        var dataOne = directMsgList.data[i];

                        // Convert UserID_PageNum into PageNum only (For Post_Id in the database)
                        // Post_Id should exist!!!
                        int underline = dataOne.id.IndexOf('_');
                        long postId = Int64.Parse(dataOne.id.Substring(underline + 1));

                        //Check the data already recorded in the database AND
                        //Check if the post includes a message => which is very important part of data
                        //
                        //Insert data to the database [25 posts per page]
                        if (!db.FBDirectMessages.Any(f => f.Id == postId))
                        {
                            #region Initialize data values

                            // Message sender's name and ID
                            string sender_name = null;
                            string sender_id = null;
                            if (dataOne.from != null)
                            {
                                sender_name = dataOne.from.name;
                                sender_id = dataOne.from.id;
                            }

                            // Message recipient's name and ID
                            string recipient1_name = null;
                            string recipient1_id = null;
                            string recipient2_name = null;
                            string recipient2_id = null;
                            if (dataOne.to != null)
                            {
                                try
                                {
                                    recipient1_id = dataOne.to.data[0].id;
                                    recipient1_name = dataOne.to.data[0].name;
                                    if ((int)dataOne.to.data.Count > 1)
                                    {
                                        recipient2_id = dataOne.to.data[1].id;
                                        recipient2_name = dataOne.to.data[1].name;
                                    }
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    continue;
                                }
                            }

                            // Message
                            string msg = null;
                            if (dataOne.message != null)
                                msg = dataOne.message;

                            // Type
                            string type = null;
                            if (dataOne.type != null)
                                type = dataOne.type;

                            // Application (iphone, android, google, etc)
                            string application = null;
                            if (dataOne.application != null)
                                application = dataOne.application.name;

                            // Created_Date
                            DateTime cr_date = default(DateTime);
                            if (dataOne.created_time != null)
                                cr_date = DateTime.Parse(dataOne.created_time);

                            // Updated_Date
                            DateTime ud_date = default(DateTime);
                            if (dataOne.updated_time != null)
                                ud_date = DateTime.Parse(dataOne.updated_time);

                            #region Likes list [25 per page]
                            var likesList = dataOne.likes;
                            int likesCount = 0;     //Total
                            int countLikeList = 0;

                            if (likesList != null)
                            {
                                while (true)
                                {
                                    try
                                    {
                                        var likeData = likesList.data[countLikeList];
                                        countLikeList++;
                                        likesCount++;

                                        #region Save the like in the database
                                        string likeId = likeData.id;
                                        string likeName = likeData.name;
                                        dynamic likeList = fb.Get("https://graph.facebook.com/" + likeId);

                                        if (likeList != null && sender_id != null && sender_name != null)
                                        {
                                            string idstr = null;
                                            if (likeList.id != null)
                                                idstr = likeList.id;
                                            string firstN = null;
                                            if (likeList.first_name != null)
                                                firstN = likeList.first_name;
                                            string lastN = null;
                                            if (likeList.last_name != null)
                                                lastN = likeList.last_name;
                                            string link = null;
                                            if (likeList.link != null)
                                                link = likeList.link;
                                            DateTime? updatedTime = default(DateTime?);
                                            if (likeList.updated_time != null)
                                                updatedTime = Convert.ToDateTime(likeList.updated_time);

                                            var alreadyin = (from t in db.FBPostLikes
                                                             where
                                                                t.Post_Id == postId
                                                                && t.Id == idstr
                                                             orderby t.Post_Id
                                                             select t.Id).FirstOrDefault();

                                            if (alreadyin == null)
                                            {
                                                try
                                                {
                                                    db.FBPostLikes.AddObject(new FBPostLike
                                                    {
                                                        Post_Id = postId,
                                                        Id = idstr,
                                                        From_User_ID = sender_id,
                                                        From_User_Name = sender_name,
                                                        FirstName = firstN,
                                                        LastName = lastN,
                                                        Link = link,
                                                        FullName = likeName,
                                                        User_Updated_Time = updatedTime
                                                    });
                                                    db.SaveChanges();
                                                }
                                                catch
                                                {
                                                    //listTimeLine.Items.Add("Update Exception occurred in DirectPostLike with Id: " + idstr + " Post_Id: " + postId);
                                                    db = new FaceBookDataCollectionEntities();
                                                    continue;
                                                }
                                            }
                                        }
                                        #endregion

                                        if (countLikeList == 25)
                                        {
                                            //string nextLikePageLink = likesList.paging.cursors.after;
                                            string linkToNext = likesList.paging.next;
                                            if (linkToNext == null) break;
                                            else
                                            {
                                                //var postId = dataOne.id;
                                                try
                                                {
                                                    likesList = fb.Get(linkToNext);
                                                    countLikeList = 0;
                                                    continue;
                                                }
                                                catch (FacebookOAuthException)
                                                {
                                                    TimeSpan _thirtySeconds = new TimeSpan(0, 0, 30);
                                                    System.Threading.Thread.Sleep(_thirtySeconds);
                                                    listTimeLine.Items.Add("While retrieving likes, took 30 sec break...");
                                                    //likesList = fb.Get(linkToNext);
                                                    //countLikeList = 0;
                                                    continue;
                                                }
                                            }
                                        }
                                    }
                                    catch (ArgumentException)
                                    {
                                        //MessageBox.Show("Like Count total is: " + likesCount);
                                        break;
                                    }
                                    catch (Exception)
                                    {
                                        TimeSpan _thirtySeconds = new TimeSpan(0, 0, 30);
                                        System.Threading.Thread.Sleep(_thirtySeconds);
                                        listTimeLine.Items.Add("While retrieving likes, took 30 sec break...");
                                        continue;
                                    }
                                }
                            }
                            #endregion

                            #region Comments List [25 per page]
                            var commentsList = dataOne.comments;
                            int commentsCount = 0;  //Total
                            int countCommentList = 0;

                            if (commentsList != null)
                            {
                                while (true)
                                {
                                    try
                                    {
                                        var commentData = commentsList.data[countCommentList];
                                        countCommentList++;
                                        commentsCount++;

                                        #region Save the comment in the database
                                        int underscore_comment = commentData.id.IndexOf('_');
                                        long commentId = Int64.Parse(commentData.id.Substring(underscore_comment + 1));

                                        if (recipient1_id != null && recipient1_name != null)
                                        {
                                            string recipientId = recipient1_id;
                                            string recipientName = recipient1_name;
                                            string writerId = null;
                                            if (commentData.from.id != null)
                                                writerId = commentData.from.id;
                                            string writerName = null;
                                            if (commentData.from.name != null)
                                                writerName = commentData.from.name;
                                            string c_message = null;
                                            if (commentData.message != null)
                                                c_message = commentData.message;
                                            long messageTagCount = 0;
                                            if (commentData.message_tags != null)
                                            {
                                                messageTagCount = commentData.message_tags.Count;
                                            }
                                            DateTime? createdDate = default(DateTime?);
                                            if (commentData.created_time != null)
                                                createdDate = Convert.ToDateTime(commentData.created_time);
                                            long likeCount = 0;
                                            if (commentData.like_count != null)
                                                likeCount = commentData.like_count;
                                            bool userLikes = false;
                                            if (commentData.user_likes != null)
                                                userLikes = true;

                                            var alreadyin = (from t in db.FBPostComments
                                                             where
                                                                t.Post_Id == postId
                                                                && t.Comment_Id == commentId
                                                                && t.Writer_ID == writerId
                                                             orderby t.Post_Id
                                                             select t.Comment_Id).FirstOrDefault();

                                            if (alreadyin == 0)
                                            {
                                                try
                                                {
                                                    db.FBPostComments.AddObject(new FBPostComment
                                                    {
                                                        Comment_Id = commentId,
                                                        Writer_ID = writerId,
                                                        Writer_Name = writerName,
                                                        Post_Id = postId,
                                                        Recipient_Id = recipientId,
                                                        Recipient_Name = recipientName,
                                                        Message = c_message,
                                                        Message_Tags_Count = messageTagCount,
                                                        Created_Time = createdDate,
                                                        Like_Count = likeCount,
                                                        User_Likes = userLikes
                                                    });
                                                    db.SaveChanges();
                                                }
                                                catch (UpdateException)
                                                {
                                                    //listTimeLine.Items.Add("Update Exception occurred in DirectPostComment with WriterName: " + writerName + " CommentID: " + commentId);
                                                    db = new FaceBookDataCollectionEntities();
                                                    continue;
                                                }
                                            }
                                        }
                                        #endregion

                                        if (countCommentList == 25)
                                        {
                                            //string nextCommentPageLink = commentsList.paging.cursors.after;
                                            string linkToNext = commentsList.paging.next;
                                            if (linkToNext == null) break;
                                            else
                                            {
                                                try
                                                {
                                                    commentsList = fb.Get(linkToNext);
                                                    countCommentList = 0;
                                                    continue;
                                                }
                                                catch (FacebookOAuthException)
                                                {
                                                    TimeSpan _thirtySeconds = new TimeSpan(0, 0, 30);
                                                    System.Threading.Thread.Sleep(_thirtySeconds);
                                                    listTimeLine.Items.Add("While retrieving comments, took 30 sec break...");
                                                    //commentsList = fb.Get(linkToNext);
                                                    //countCommentList = 0;
                                                    continue;
                                                }
                                            }
                                        }
                                    }
                                    catch (ArgumentException)
                                    {
                                        //MessageBox.Show("Comment Count total is: " + commentsList);
                                        break;
                                    }
                                    catch (Exception)
                                    {
                                        TimeSpan _thirtySeconds = new TimeSpan(0, 0, 30);
                                        System.Threading.Thread.Sleep(_thirtySeconds);
                                        listTimeLine.Items.Add("While retrieving likes, took 30 sec break...");
                                        continue;
                                    }
                                }
                            }
                            #endregion
                            #endregion

                            db.FBDirectMessages.AddObject(new FBDirectMessage
                            {
                                Id = postId,
                                Sender_Id = sender_id,
                                Sender_Name = sender_name,
                                Recipient1_Id = recipient1_id,
                                Recipient1_Name = recipient1_name,
                                Recipient2_Id = recipient2_id,
                                Recipient2_Name = recipient2_name,
                                Message = msg,
                                Type = type,
                                Application = application,
                                Created_Time = cr_date,
                                Updated_Time = ud_date,
                                Likes = likesCount,
                                Comments = commentsCount
                            });
                            try
                            {
                                recordsAffected += db.SaveChanges();
                                numPosts++;
                            }
                            catch (UpdateException)
                            {
                                duplicatesFound++;
                            }
                        }
                        else
                        {
                            /* Update existing posts only which are saved a month ago */
                            var existing = (from u in db.FBDirectMessages where u.Id == postId select u).FirstOrDefault();
                            if (existing != null)
                            {
                                DateTime thisDay = DateTime.Today;
                                DateTime updated_Time = existing.Updated_Time.GetValueOrDefault(DateTime.Now);
                                var diffHours = (thisDay - updated_Time).TotalHours;
                                if (diffHours < 1464)
                                {
                                    #region Initialize data values

                                    // Message sender's name and ID
                                    string sender_name = null;
                                    string sender_id = null;
                                    if (dataOne.from != null)
                                    {
                                        sender_name = dataOne.from.name;
                                        sender_id = dataOne.from.id;
                                    }

                                    // Message recipient's name and ID
                                    string recipient1_name = null;
                                    string recipient1_id = null;
                                    string recipient2_name = null;
                                    string recipient2_id = null;
                                    if (dataOne.to != null)
                                    {
                                        try
                                        {
                                            recipient1_id = dataOne.to.data[0].id;
                                            recipient1_name = dataOne.to.data[0].name;
                                            if ((int)dataOne.to.data.Count > 1)
                                            {
                                                recipient2_id = dataOne.to.data[1].id;
                                                recipient2_name = dataOne.to.data[1].name;
                                            }
                                        }
                                        catch (ArgumentOutOfRangeException)
                                        {
                                            continue;
                                        }
                                    }

                                    // Message
                                    string msg = null;
                                    if (dataOne.message != null)
                                        msg = dataOne.message;

                                    // Type
                                    string type = null;
                                    if (dataOne.type != null)
                                        type = dataOne.type;

                                    // Application (iphone, android, google, etc)
                                    string application = null;
                                    if (dataOne.application != null)
                                        application = dataOne.application.name;

                                    // Created_Date
                                    DateTime cr_date = default(DateTime);
                                    if (dataOne.created_time != null)
                                        cr_date = DateTime.Parse(dataOne.created_time);

                                    // Updated_Date
                                    DateTime ud_date = default(DateTime);
                                    if (dataOne.updated_time != null)
                                        ud_date = DateTime.Parse(dataOne.updated_time);

                                    #region Likes list [25 per page]
                                    var likesList = dataOne.likes;
                                    int likesCount = 0;     //Total
                                    int countLikeList = 0;

                                    if (likesList != null)
                                    {
                                        while (true)
                                        {
                                            try
                                            {
                                                var likeData = likesList.data[countLikeList];
                                                countLikeList++;
                                                likesCount++;

                                                #region Save the like in the database
                                                string likeId = likeData.id;
                                                string likeName = likeData.name;
                                                dynamic likeList = fb.Get("https://graph.facebook.com/" + likeId);

                                                if (likeList != null && sender_id != null && sender_name != null)
                                                {
                                                    string idstr = null;
                                                    if (likeList.id != null)
                                                        idstr = likeList.id;
                                                    string firstN = null;
                                                    if (likeList.first_name != null)
                                                        firstN = likeList.first_name;
                                                    string lastN = null;
                                                    if (likeList.last_name != null)
                                                        lastN = likeList.last_name;
                                                    string link = null;
                                                    if (likeList.link != null)
                                                        link = likeList.link;
                                                    DateTime? updatedTime = default(DateTime?);
                                                    if (likeList.updated_time != null)
                                                        updatedTime = Convert.ToDateTime(likeList.updated_time);

                                                    var alreadyin = (from t in db.FBPostLikes
                                                                     where
                                                                        t.Post_Id == postId
                                                                        && t.Id == idstr
                                                                     orderby t.Post_Id
                                                                     select t.Id).FirstOrDefault();

                                                    if (alreadyin == null)
                                                    {
                                                        try
                                                        {
                                                            db.FBPostLikes.AddObject(new FBPostLike
                                                            {
                                                                Post_Id = postId,
                                                                Id = idstr,
                                                                From_User_ID = sender_id,
                                                                From_User_Name = sender_name,
                                                                FirstName = firstN,
                                                                LastName = lastN,
                                                                Link = link,
                                                                FullName = likeName,
                                                                User_Updated_Time = updatedTime
                                                            });
                                                            db.SaveChanges();
                                                        }
                                                        catch
                                                        {
                                                            //listTimeLine.Items.Add("Update Exception occurred in DirectPostLike with Id: " + idstr + " Post_Id: " + postId);
                                                            db = new FaceBookDataCollectionEntities();
                                                            continue;
                                                        }
                                                    }
                                                }
                                                #endregion

                                                if (countLikeList == 25)
                                                {
                                                    //string nextLikePageLink = likesList.paging.cursors.after;
                                                    string linkToNext = likesList.paging.next;
                                                    if (linkToNext == null) break;
                                                    else
                                                    {
                                                        //var postId = dataOne.id;
                                                        try
                                                        {
                                                            likesList = fb.Get(linkToNext);
                                                            countLikeList = 0;
                                                            continue;
                                                        }
                                                        catch (FacebookOAuthException)
                                                        {
                                                            TimeSpan _thirtySeconds = new TimeSpan(0, 0, 30);
                                                            System.Threading.Thread.Sleep(_thirtySeconds);
                                                            listTimeLine.Items.Add("While retrieving likes, took 30 sec break...");
                                                            //likesList = fb.Get(linkToNext);
                                                            //countLikeList = 0;
                                                            continue;
                                                        }
                                                    }
                                                }
                                            }
                                            catch (ArgumentException)
                                            {
                                                //MessageBox.Show("Like Count total is: " + likesCount);
                                                break;
                                            }
                                            catch (Exception)
                                            {
                                                TimeSpan _thirtySeconds = new TimeSpan(0, 0, 30);
                                                System.Threading.Thread.Sleep(_thirtySeconds);
                                                listTimeLine.Items.Add("While retrieving likes, took 30 sec break...");
                                                continue;
                                            }
                                        }
                                    }
                                    #endregion

                                    #region Comments List [25 per page]
                                    var commentsList = dataOne.comments;
                                    int commentsCount = 0;  //Total
                                    int countCommentList = 0;

                                    if (commentsList != null)
                                    {
                                        while (true)
                                        {
                                            try
                                            {
                                                var commentData = commentsList.data[countCommentList];
                                                countCommentList++;
                                                commentsCount++;

                                                #region Save the comment in the database
                                                int underscore_comment = commentData.id.IndexOf('_');
                                                long commentId = Int64.Parse(commentData.id.Substring(underscore_comment + 1));

                                                if (recipient1_id != null && recipient1_name != null)
                                                {
                                                    string recipientId = recipient1_id;
                                                    string recipientName = recipient1_name;
                                                    string writerId = null;
                                                    if (commentData.from.id != null)
                                                        writerId = commentData.from.id;
                                                    string writerName = null;
                                                    if (commentData.from.name != null)
                                                        writerName = commentData.from.name;
                                                    string c_message = null;
                                                    if (commentData.message != null)
                                                        c_message = commentData.message;
                                                    long messageTagCount = 0;
                                                    if (commentData.message_tags != null)
                                                    {
                                                        messageTagCount = commentData.message_tags.Count;
                                                    }
                                                    DateTime? createdDate = default(DateTime?);
                                                    if (commentData.created_time != null)
                                                        createdDate = Convert.ToDateTime(commentData.created_time);
                                                    long likeCount = 0;
                                                    if (commentData.like_count != null)
                                                        likeCount = commentData.like_count;
                                                    bool userLikes = false;
                                                    if (commentData.user_likes != null)
                                                        userLikes = true;

                                                    var alreadyin = (from t in db.FBPostComments
                                                                     where
                                                                        t.Post_Id == postId
                                                                        && t.Comment_Id == commentId
                                                                        && t.Writer_ID == writerId
                                                                     orderby t.Post_Id
                                                                     select t.Comment_Id).FirstOrDefault();

                                                    if (alreadyin == 0)
                                                    {
                                                        try
                                                        {
                                                            db.FBPostComments.AddObject(new FBPostComment
                                                            {
                                                                Comment_Id = commentId,
                                                                Writer_ID = writerId,
                                                                Writer_Name = writerName,
                                                                Post_Id = postId,
                                                                Recipient_Id = recipientId,
                                                                Recipient_Name = recipientName,
                                                                Message = c_message,
                                                                Message_Tags_Count = messageTagCount,
                                                                Created_Time = createdDate,
                                                                Like_Count = likeCount,
                                                                User_Likes = userLikes
                                                            });
                                                            db.SaveChanges();
                                                        }
                                                        catch (UpdateException)
                                                        {
                                                            //listTimeLine.Items.Add("Update Exception occurred in DirectPostComment with WriterName: " + writerName + " CommentID: " + commentId);
                                                            db = new FaceBookDataCollectionEntities();
                                                            continue;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        try
                                                        {
                                                            /* for existing comments */
                                                            var comment_exists = (from u in db.FBPostComments where u.Comment_Id == commentId select u).FirstOrDefault();
                                                            if (comment_exists != null)
                                                            {
                                                                DateTime created_time_comment = existing.Updated_Time.GetValueOrDefault(DateTime.Now);
                                                                var diffHours_comment = (thisDay - created_time_comment).TotalHours;
                                                                if (diffHours_comment < 1464)
                                                                {

                                                                    if (comment_exists.Message != c_message)
                                                                        comment_exists.Message = c_message;
                                                                    if (comment_exists.Message_Tags_Count != messageTagCount)
                                                                        comment_exists.Message_Tags_Count = messageTagCount;
                                                                    if (comment_exists.Like_Count != likeCount)
                                                                        comment_exists.Like_Count = likeCount;
                                                                    if (comment_exists.User_Likes != userLikes)
                                                                        comment_exists.User_Likes = userLikes;

                                                                    db.SaveChanges();
                                                                }
                                                            }
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            MessageBox.Show("Error occurred in Posts - Comments: " + ex);
                                                        }
                                                    }
                                                }
                                                #endregion

                                                if (countCommentList == 25)
                                                {
                                                    //string nextCommentPageLink = commentsList.paging.cursors.after;
                                                    string linkToNext = commentsList.paging.next;
                                                    if (linkToNext == null) break;
                                                    else
                                                    {
                                                        try
                                                        {
                                                            commentsList = fb.Get(linkToNext);
                                                            countCommentList = 0;
                                                            continue;
                                                        }
                                                        catch (FacebookOAuthException)
                                                        {
                                                            TimeSpan _thirtySeconds = new TimeSpan(0, 0, 30);
                                                            System.Threading.Thread.Sleep(_thirtySeconds);
                                                            listTimeLine.Items.Add("While retrieving comments, took 30 sec break...");
                                                            //commentsList = fb.Get(linkToNext);
                                                            //countCommentList = 0;
                                                            continue;
                                                        }
                                                    }
                                                }
                                            }
                                            catch (ArgumentException)
                                            {
                                                //MessageBox.Show("Comment Count total is: " + commentsList);
                                                break;
                                            }
                                            catch (Exception)
                                            {
                                                TimeSpan _thirtySeconds = new TimeSpan(0, 0, 30);
                                                System.Threading.Thread.Sleep(_thirtySeconds);
                                                listTimeLine.Items.Add("While retrieving likes, took 30 sec break...");
                                                continue;
                                            }
                                        }
                                    }
                                    #endregion
                                    #endregion

                                    if (existing.Recipient1_Id != recipient1_id)
                                        existing.Recipient1_Id = recipient1_id;
                                    if (existing.Recipient1_Name != recipient1_name)
                                        existing.Recipient1_Name = recipient1_name;
                                    if (existing.Recipient2_Id != recipient2_id)
                                        existing.Recipient2_Id = recipient2_id;
                                    if (existing.Recipient2_Name != recipient2_name)
                                        existing.Recipient2_Name = recipient2_name;
                                    if (existing.Message != msg)
                                        existing.Message = msg;
                                    if (existing.Type != type)
                                        existing.Type = type;
                                    if (existing.Updated_Time != ud_date)
                                        existing.Updated_Time = ud_date;
                                    if (existing.Likes != likesCount)
                                        existing.Likes = likesCount;
                                    if (existing.Comments != commentsCount)
                                        existing.Comments = commentsCount;

                                    db.SaveChanges();
                                }
                            }
                            duplicatesFound++;
                        }
                    }

                    listTimeLine.Items.Add("Saved " + numPosts + " posts so far to the database (" + recordsAffected + " entities total); " + duplicatesFound + " duplicates not saved");
                    //MessageBox.Show("Saved " + numPosts + " posts so far to the database (" + recordsAffected + " entities total); " + duplicatesFound + " duplicates not saved");

                    // Continue to next page
                    if (directMsgList.paging != null)
                    {
                        try
                        {
                            // GET NEXT PAGE DATA
                            string nextPageLink = directMsgList.paging.next;
                            int uniqueSymbol = nextPageLink.LastIndexOf("&");
                            if (uniqueSymbol != -1)
                            {
                                nextPageLink = nextPageLink.Substring(0, uniqueSymbol);
                                int equalSymbol = nextPageLink.LastIndexOf("=");
                                if (equalSymbol != -1)
                                {
                                    string nextPageNum = nextPageLink.Substring(equalSymbol + 1);

                                    directMsgList = fb.Get("/" + userAccountName + "/feed?until=" + nextPageNum);

                                    // GET PREVIOUS PAGE DATA
                                    //string prevPageLink = directMsgList.paging.previous;
                                    //int uniqueSymbol = prevPageLink.LastIndexOf("&");
                                    //string temp = prevPageLink.Substring(uniqueSymbol - 10, 10);
                                    //long prevPageNum = long.Parse(prevPageLink.Substring(uniqueSymbol - 10, 10));
                                    //directMsgList = fb.Get("/"+userAccountName+"/feed?since=" + prevPageNum);

                                    count = (int)directMsgList.data.Count;
                                }
                            }

                            if (count <= 0)
                            {
                                MessageBox.Show("No more Direct Message data available");
                                break;
                            }
                            else continue;
                        }
                        catch (FacebookOAuthException)
                        {
                            TimeSpan _thirtySeconds = new TimeSpan(0, 0, 30);
                            System.Threading.Thread.Sleep(_thirtySeconds);
                            listTimeLine.Items.Add("User Request limit reached while retrieving direct messages, took 30 sec break...");
                            continue;
                        }
                    }
                }
            }
            catch (FacebookOAuthException ex)
            {
                listTimeLine.Items.Add("Error: " + ex);
            }
        }

        private void analysePosNeg_Click(object sender, EventArgs e)
        {
            try
            {
                var db = new FaceBookDataCollectionEntities();
                int added = 0; int duplicate = 0; int recordsAffected = 0;

                // Positive and Negative Links, get their lists from webpage
                // e.g. http://www3.nd.edu/~mcdonald/Word_Lists.html
                listTimeLine.Items.Add("[" + DateTime.Now + "] Process Starts. Please wait for a few minutes.");
                string word = ""; int posCount = 0; int negCount = 0;

                // Positive Words
                string positiveWordLink = "http://www3.nd.edu/~mcdonald/Data/Finance_Word_Lists/LoughranMcDonald_Positive.csv";
                string positiveCode = WorkerClasses.getSourceCode(positiveWordLink).ToLower().Replace("\n", "");
                if (positiveCode == "invalid") throw new UriFormatException();
                string positiveCode_copy = positiveCode;

                // Negative Words
                string negativeWordLink = "http://www3.nd.edu/~mcdonald/Data/Finance_Word_Lists/LoughranMcDonald_Negative.csv";
                string negativeCode = WorkerClasses.getSourceCode(negativeWordLink).ToLower().Replace("\n", "");
                if (negativeCode == "invalid") throw new UriFormatException();
                string negativeCode_copy = negativeCode;

                ////////////////////////////////////////////////////////////////////////////////////////////////
                //....................  1. FBDirect Message    .................................................
                ////////////////////////////////////////////////////////////////////////////////////////////////
                var directMessages = (from u in db.FBDirectMessages select u).ToList();

                foreach (var u in directMessages)
                {
                    long userId = u.Id;
                    if (!db.FBDirectMessage_PosNeg.Any(f => f.Id == userId))
                    {
                        #region Pos/Neg Words - DirectMessage
                        string content = u.Message;
                        if (content != null && content != "")
                        {
                            string[] contentSplit = content.Split(new char[] { '.', '?', '!', ' ', ';', ':', ',' }, StringSplitOptions.RemoveEmptyEntries);
                            // Select a word (read by line) in the list
                            while (positiveCode.IndexOf("\r") != -1)
                            {
                                int endIndex = positiveCode.IndexOf("\r");
                                word = positiveCode.Substring(0, endIndex);
                                positiveCode = positiveCode.Substring(endIndex + 1, positiveCode.Length - endIndex - 1);
                                if (content.Contains(word))
                                {
                                    var matchQuery = from words in contentSplit
                                                     where words.ToLowerInvariant() == word.ToLowerInvariant()
                                                     select words;
                                    posCount += matchQuery.Count();
                                }
                            }
                            while (negativeCode.IndexOf("\r") != -1)
                            {
                                int endIndex = negativeCode.IndexOf("\r");
                                word = negativeCode.Substring(0, endIndex);
                                negativeCode = negativeCode.Substring(endIndex + 1, negativeCode.Length - endIndex - 1);
                                if (content.Contains(word))
                                {
                                    var matchQuery = from words in contentSplit
                                                     where words.ToLowerInvariant() == word.ToLowerInvariant()
                                                     select words;
                                    negCount += matchQuery.Count();
                                }
                            }
                        }
                        #endregion

                        int postWordCount = 0;
                        if (content != null && content != "") 
                            postWordCount = Regex.Matches(u.Message, @"[\S]+").Count; 

                        db.FBDirectMessage_PosNeg.AddObject(new FBDirectMessage_PosNeg
                        {
                            Id = u.Id,
                            Sender_Id = u.Sender_Id,
                            Sender_Name = u.Sender_Name,
                            Recipient1_Id = u.Recipient1_Id,
                            Recipient1_Name = u.Recipient1_Name,
                            Recipient2_Id = u.Recipient2_Id,
                            Recipient2_Name = u.Recipient2_Name,
                            Message = u.Message,
                            Type = u.Type,
                            Application = u.Application,
                            Created_Time = u.Created_Time,
                            Updated_Time = u.Updated_Time,
                            Likes = u.Likes,
                            Comments = u.Comments,
                            PosWords = posCount,
                            NegWords = negCount,
                            Length_of_Message = postWordCount
                        });
                        db.SaveChanges();
                        added++;

                        // Reset components for next loop
                        positiveCode = positiveCode_copy;
                        negativeCode = negativeCode_copy;
                        posCount = 0;
                        negCount = 0;
                    }
                    else
                        duplicate++;
                }
                listTimeLine.Items.Add("[" + DateTime.Now + "] " + added + " DirectMessage Pos/Neg Saved (" 
                    + recordsAffected + " total); " + duplicate + " duplicates not saved.");
                added = 0; duplicate = 0; recordsAffected = 0;

                ////////////////////////////////////////////////////////////////////////////////////////////////
                //....................  2. FBPostsComments    .................................................
                ////////////////////////////////////////////////////////////////////////////////////////////////
                var postcomments = (from u in db.FBPostComments select u).ToList();

                foreach (var u in postcomments)
                {
                    long userId = u.Comment_Id;
                    if (!db.FBPostComments_PosNeg.Any(f => f.Comment_Id == userId))
                    {
                        #region Pos/Neg Words - PostComments
                        string content = u.Message;
                        if (content != null && content != "")
                        {
                            string[] contentSplit = content.Split(new char[] { '.', '?', '!', ' ', ';', ':', ',' }, StringSplitOptions.RemoveEmptyEntries);
                            // Select a word (read by line) in the list
                            while (positiveCode.IndexOf("\r") != -1)
                            {
                                int endIndex = positiveCode.IndexOf("\r");
                                word = positiveCode.Substring(0, endIndex);
                                positiveCode = positiveCode.Substring(endIndex + 1, positiveCode.Length - endIndex - 1);
                                if (content.Contains(word))
                                {
                                    var matchQuery = from words in contentSplit
                                                     where words.ToLowerInvariant() == word.ToLowerInvariant()
                                                     select words;
                                    posCount += matchQuery.Count();
                                }
                            }
                            while (negativeCode.IndexOf("\r") != -1)
                            {
                                int endIndex = negativeCode.IndexOf("\r");
                                word = negativeCode.Substring(0, endIndex);
                                negativeCode = negativeCode.Substring(endIndex + 1, negativeCode.Length - endIndex - 1);
                                if (content.Contains(word))
                                {
                                    var matchQuery = from words in contentSplit
                                                     where words.ToLowerInvariant() == word.ToLowerInvariant()
                                                     select words;
                                    negCount += matchQuery.Count();
                                }
                            }
                        }
                        #endregion

                        int postWordCount = 0;
                        if (content != null && content != "")
                            postWordCount = Regex.Matches(u.Message, @"[\S]+").Count;

                        db.FBPostComments_PosNeg.AddObject(new FBPostComments_PosNeg
                        {
                            Comment_Id = u.Comment_Id,
                            Writer_ID = u.Writer_ID,
                            Writer_Name = u.Writer_Name,
                            Post_Id = u.Post_Id,
                            Recipient_Id = u.Recipient_Id,
                            Recipient_Name = u.Recipient_Name,
                            Message = u.Message,
                            Message_Tags_Count = u.Message_Tags_Count,
                            Created_Time = u.Created_Time,
                            Like_Count = u.Like_Count,
                            User_Likes = u.User_Likes,
                            PosWords = posCount,
                            NegWords = negCount,
                            Length_of_Message = postWordCount
                        });
                        db.SaveChanges();
                        added++;

                        // Reset components for next loop
                        positiveCode = positiveCode_copy;
                        negativeCode = negativeCode_copy;
                        posCount = 0;
                        negCount = 0;
                    }
                    else
                        duplicate++;
                }
                listTimeLine.Items.Add("[" + DateTime.Now + "] " + added + " PostComments Pos/Neg Saved ("
                    + recordsAffected + " total); " + duplicate + " duplicates not saved.");
                added = 0; duplicate = 0; recordsAffected = 0;

                ////////////////////////////////////////////////////////////////////////////////////////////////
                //....................  3. FBPosts    .................................................
                ////////////////////////////////////////////////////////////////////////////////////////////////
                var posts = (from u in db.FBPosts select u).ToList();

                foreach (var u in posts)
                {
                    long userId = u.Post_Id;
                    if (!db.FBPosts_PosNeg.Any(f => f.Post_Id == userId))
                    {
                        #region Pos/Neg Words - PostComments
                        string content = u.Message;
                        if (content != null && content != "")
                        {
                            string[] contentSplit = content.Split(new char[] { '.', '?', '!', ' ', ';', ':', ',' }, StringSplitOptions.RemoveEmptyEntries);
                            // Select a word (read by line) in the list
                            while (positiveCode.IndexOf("\r") != -1)
                            {
                                int endIndex = positiveCode.IndexOf("\r");
                                word = positiveCode.Substring(0, endIndex);
                                positiveCode = positiveCode.Substring(endIndex + 1, positiveCode.Length - endIndex - 1);
                                if (content.Contains(word))
                                {
                                    var matchQuery = from words in contentSplit
                                                     where words.ToLowerInvariant() == word.ToLowerInvariant()
                                                     select words;
                                    posCount += matchQuery.Count();
                                }
                            }
                            while (negativeCode.IndexOf("\r") != -1)
                            {
                                int endIndex = negativeCode.IndexOf("\r");
                                word = negativeCode.Substring(0, endIndex);
                                negativeCode = negativeCode.Substring(endIndex + 1, negativeCode.Length - endIndex - 1);
                                if (content.Contains(word))
                                {
                                    var matchQuery = from words in contentSplit
                                                     where words.ToLowerInvariant() == word.ToLowerInvariant()
                                                     select words;
                                    negCount += matchQuery.Count();
                                }
                            }
                        }
                        #endregion

                        int postWordCount = 0;
                        if (content != null && content != "")
                            postWordCount = Regex.Matches(u.Message, @"[\S]+").Count;

                        db.FBPosts_PosNeg.AddObject(new FBPosts_PosNeg
                        {
                            Post_Id = u.Post_Id,
                            User_Name = u.User_Name,
                            User_Id = u.User_Id,
                            Category = u.Category,
                            Message = u.Message,
                            Picture = u.Picture,
                            Link = u.Link,
                            Created_Time = u.Created_Time,
                            Updated_Time = u.Updated_Time,
                            Type = u.Type,
                            Status_Type = u.Status_Type,
                            Shares = u.Shares,
                            Likes = u.Likes,
                            Comments = u.Comments,
                            PostByOther_Id = u.PostByOther_Id,
                            PostByOther_Name = u.PostByOther_Name,
                            Caption = u.Caption,
                            Caption_Title = u.Caption_Title,
                            Cap_Description = u.Cap_Description,
                            PosWords = posCount,
                            NegWords = negCount,
                            Length_of_Message = postWordCount
                        });
                        db.SaveChanges();
                        added++;

                        // Reset components for next loop
                        positiveCode = positiveCode_copy;
                        negativeCode = negativeCode_copy;
                        posCount = 0;
                        negCount = 0;
                    }
                    else
                        duplicate++;
                }
                listTimeLine.Items.Add("[" + DateTime.Now + "] " + added + " Posts Pos/Neg Saved ("
                    + recordsAffected + " total); " + duplicate + " duplicates not saved.");
                added = 0; duplicate = 0; recordsAffected = 0;
            }
            catch (Exception ex)
            {
                listTimeLine.Items.Add("[" + DateTime.Now + "] Error: " + ex);
                MessageBox.Show("Error: " + ex);
            }
        }
    }
}


// By Michael - Mickoon
