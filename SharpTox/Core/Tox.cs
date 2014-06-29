#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;

namespace SharpTox.Core
{
    #region Event Delegates
    public delegate void OnFriendRequestDelegate(string id, string message);
    public delegate void OnConnectionStatusDelegate(ToxFriend friend, int status);
    public delegate void OnFriendMessageDelegate(ToxFriend friend, string message);
    public delegate void OnFriendActionDelegate(ToxFriend friend, string action);
    public delegate void OnNameChangeDelegate(ToxFriend friend, string newname);
    public delegate void OnStatusMessageDelegate(ToxFriend friend, string newstatus);
    public delegate void OnUserStatusDelegate(ToxFriend friend, ToxUserStatus status);
    public delegate void OnTypingChangeDelegate(ToxFriend friend, bool is_typing);

    public delegate void OnGroupInviteDelegate(ToxFriend friend, string group_public_key);
    public delegate void OnGroupMessageDelegate(ToxGroup group, int friendgroupnumber, string message);
    public delegate void OnGroupActionDelegate(ToxGroup group, int friendgroupnumber, string action);
    public delegate void OnGroupNamelistChangeDelegate(ToxGroup group, int peernumber, ToxChatChange change);

    public delegate void OnFileControlDelegate(ToxFriend friend, int receive_send, int filenumber, int control_type, byte[] data);
    public delegate void OnFileDataDelegate(ToxFriend friend, int filenumber, byte[] data);
    public delegate void OnFileSendRequestDelegate(ToxFriend friend, int filenumber, ulong filesize, string filename);
    public delegate void OnReadReceiptDelegate(ToxFriend friend, uint receipt);
    #endregion

    public class Tox
    {
        /// <summary>
        /// Occurs when a friend request is received.
        /// </summary>
        public event OnFriendRequestDelegate OnFriendRequest;

        /// <summary>
        /// Occurs when the connection status of a friend has changed.
        /// </summary>
        public event OnConnectionStatusDelegate OnConnectionStatusChanged;

        /// <summary>
        /// Occurs when a message is received from a friend.
        /// </summary>
        public event OnFriendMessageDelegate OnFriendMessage;

        /// <summary>
        /// Occurs when an action is received from a friend.
        /// </summary>
        public event OnFriendActionDelegate OnFriendAction;

        /// <summary>
        /// Occurs when a friend has changed his/her name.
        /// </summary>
        public event OnNameChangeDelegate OnNameChange;

        /// <summary>
        /// Occurs when a friend has changed their status message.
        /// </summary>
        public event OnStatusMessageDelegate OnStatusMessage;

        /// <summary>
        /// Occurs when a friend has changed their user status.
        /// </summary>
        public event OnUserStatusDelegate OnUserStatus;

        /// <summary>
        /// Occurs when a friend's typing status has changed.
        /// </summary>
        public event OnTypingChangeDelegate OnTypingChange;

        /// <summary>
        /// Occurs when an action is received from a group.
        /// </summary>
        public event OnGroupActionDelegate OnGroupAction;

        /// <summary>
        /// Occurs when a message is received from a group.
        /// </summary>
        public event OnGroupMessageDelegate OnGroupMessage;

        /// <summary>
        /// Occurs when a friend has sent an invite to a group.
        /// </summary>
        public event OnGroupInviteDelegate OnGroupInvite;

        /// <summary>
        /// Occurs when the name list of a group has changed.
        /// </summary>
        public event OnGroupNamelistChangeDelegate OnGroupNamelistChange;

        /// <summary>
        /// Occurs when a file control request is received.
        /// </summary>
        public event OnFileControlDelegate OnFileControl;

        /// <summary>
        /// Occurs when file data is received.
        /// </summary>
        public event OnFileDataDelegate OnFileData;

        /// <summary>
        /// Occurs when a file send request is received.
        /// </summary>
        public event OnFileSendRequestDelegate OnFileSendRequest;

        /// <summary>
        /// Occurs when a read receipt is received.
        /// </summary>
        public event OnReadReceiptDelegate OnReadReceipt;

        public delegate object InvokeDelegate(Delegate method, params object[] p);

        /// <summary>
        /// The invoke delegate to use when raising events.
        /// </summary>
        public InvokeDelegate Invoker { get; private set; }

        private List<ToxFriend> friends;
        private List<ToxGroup> groups;
        public ReadOnlyCollection<ToxFriend> Friends 
        { 
            get 
            { 
                return friends.AsReadOnly();
            }
        }
        public ReadOnlyCollection<ToxGroup> Groups
        {
            get
            {
                return groups.AsReadOnly();
            }
        }

        #region Callback Delegates
        private ToxDelegates.CallbackFriendRequestDelegate friendrequestdelegate;
        private ToxDelegates.CallbackConnectionStatusDelegate connectionstatusdelegate;
        private ToxDelegates.CallbackFriendMessageDelegate friendmessagedelegate;
        private ToxDelegates.CallbackFriendActionDelegate friendactiondelegate;
        private ToxDelegates.CallbackNameChangeDelegate namechangedelegate;
        private ToxDelegates.CallbackStatusMessageDelegate statusmessagedelegate;
        private ToxDelegates.CallbackUserStatusDelegate userstatusdelegate;
        private ToxDelegates.CallbackTypingChangeDelegate typingchangedelegate;

        private ToxDelegates.CallbackGroupInviteDelegate groupinvitedelegate;
        private ToxDelegates.CallbackGroupActionDelegate groupactiondelegate;
        private ToxDelegates.CallbackGroupMessageDelegate groupmessagedelegate;
        private ToxDelegates.CallbackGroupNamelistChangeDelegate groupnamelistchangedelegate;

        private ToxDelegates.CallbackFileControlDelegate filecontroldelegate;
        private ToxDelegates.CallbackFileDataDelegate filedatadelegate;
        private ToxDelegates.CallbackFileSendRequestDelegate filesendrequestdelegate;

        private ToxDelegates.CallbackReadReceiptDelegate readreceiptdelegate;
        #endregion

        private IntPtr tox;
        private Thread thread;

        private object obj;

        /// <summary>
        /// Setting this to false will make sure that resolving sticks strictly to IPv4 addresses.
        /// </summary>
        public bool Ipv6Enabled { get; private set; }

        /// <summary>
        /// Initializes a new instance of tox.
        /// </summary>
        /// <param name="ipv6enabled"></param>
        public Tox(bool ipv6enabled)
        {
            tox = ToxFunctions.New(ipv6enabled);
            Ipv6Enabled = ipv6enabled;

            friends = new List<ToxFriend>();
            groups = new List<ToxGroup>();
            obj = new object();
            Invoker = new InvokeDelegate(dummyinvoker);

            callbacks();
        }

        private object dummyinvoker(Delegate method, params object[] p)
        {
            return method.DynamicInvoke(p);
        }

        /// <summary>
        /// Check whether we are connected to the DHT.
        /// </summary>
        /// <returns>true if we are and false if we aren't.</returns>
        public bool IsConnected()
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.IsConnected(tox);
            }
        }

        /// <summary>
        /// Sends a file send request to the given friendnumber.
        /// </summary>
        /// <param name="friend"></param>
        /// <param name="filesize"></param>
        /// <param name="filename">Maximum filename length is 255 bytes.</param>
        /// <returns>the filenumber on success and -1 on failure.</returns>
        public int NewFileSender(ToxFriend friend, ulong filesize, string filename)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.NewFileSender(tox, friend.Number, filesize, filename);
            }
        }

        /// <summary>
        /// Sends a file control request.
        /// </summary>
        /// <param name="friend"></param>
        /// <param name="send_receive">0 if we're sending and 1 if we're receiving.</param>
        /// <param name="filenumber"></param>
        /// <param name="message_id"></param>
        /// <param name="data"></param>
        /// <returns>true on success and false on failure.</returns>
        public bool FileSendControl(ToxFriend friend, int send_receive, int filenumber, ToxFileControl message_id, byte[] data)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.FileSendControl(tox, friend.Number, (byte)send_receive, (byte)filenumber, (byte)message_id, data, (ushort)data.Length);
            }
        }

        /// <summary>
        /// Sends file data.
        /// </summary>
        /// <param name="friend"></param>
        /// <param name="filenumber"></param>
        /// <param name="data"></param>
        /// <returns>true on success and false on failure.</returns>
        public bool FileSendData(ToxFriend friend, int filenumber, byte[] data)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.FileSendData(tox, friend.Number, filenumber, data);
            }
        }

        /// <summary>
        /// Retrieves the recommended/maximum size of the filedata to send with FileSendData.
        /// </summary>
        /// <param name="friend"></param>
        /// <returns></returns>
        public int FileDataSize(ToxFriend friend)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.FileDataSize(tox, friend.Number);
            }
        }

        /// <summary>
        /// Retrieves the number of bytes left to be sent/received.
        /// </summary>
        /// <param name="friend"></param>
        /// <param name="filenumber"></param>
        /// <param name="send_receive">0 if we're sending and 1 if we're receiving.</param>
        /// <returns></returns>
        public ulong FileDataRemaining(ToxFriend friend, int filenumber, int send_receive)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.FileDataRemaining(tox, friend.Number, filenumber, send_receive);
            }
        }

        /// <summary>
        /// Loads the tox data file from a location specified by filename.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool Load(string filename)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                try
                {
                    FileInfo info = new FileInfo(filename);
                    FileStream stream = new FileStream(filename, FileMode.Open);
                    byte[] bytes = new byte[info.Length];

                    stream.Read(bytes, 0, (int)info.Length);
                    stream.Close();

                    if (!ToxFunctions.Load(tox, bytes, (uint)bytes.Length))
                        return false;
                    else
                        return true;
                }
                catch { return false; }
            }
        }

        /// <summary>
        /// Retrieves an array of group member names. Not implemented yet.
        /// </summary>
        /// <param name="groupnumber"></param>
        /// <returns></returns>
        private string[] GetGroupNames(int groupnumber)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.GroupGetNames(tox, groupnumber);
            }
        }

        /// <summary>
        /// Starts the main tox_do loop.
        /// </summary>
        public void Start()
        {
            thread = new Thread(loop);
            thread.Start();
        }

        private void loop()
        {
            while (true)
            {
                //tox_do should be called at least 20 times per second
                ToxFunctions.Do(tox);
                Thread.Sleep(25);
            }
        }

        /// <summary>
        /// Adds a friend.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="message"></param>
        /// <returns>friendnumber</returns>
        public ToxFriend AddFriend(string id, string message)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                int result = ToxFunctions.AddFriend(tox, id, message);

                if (result < 0)
                {
                    throw new Exception("Could not add friend: " + (ToxAFError)result);
                }
                else
                {
                    ToxFriend friend = new ToxFriend(result);
                    friends.Add(friend);

                    return friend;
                }
            }
        }

        /// <summary>
        /// Adds a friend with a default message.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>friendnumber</returns>
        public ToxFriend AddFriend(string id)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                int result = ToxFunctions.AddFriend(tox, id, "No message.");

                if (result < 0)
                {
                    throw new Exception("Could not add friend: " + (ToxAFError)result);
                }
                else
                {
                    ToxFriend friend = new ToxFriend(result);
                    friends.Add(friend);

                    return friend;
                }
            }
        }

        /// <summary>
        /// Adds a friend without sending a request.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>friendnumber</returns>
        public ToxFriend AddFriendNoRequest(string id)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                int result = ToxFunctions.AddFriendNoRequest(tox, id);

                if (result < 0)
                {
                    throw new Exception("Could not add friend: " + (ToxAFError)result);
                }
                else
                {
                    ToxFriend friend = new ToxFriend(result);
                    friends.Add(friend);

                    return friend;
                }
            }
        }

        /// <summary>
        /// Bootstraps the tox client with a ToxNode.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool BootstrapFromNode(ToxNode node)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.BootstrapFromAddress(tox, node.Address, node.Ipv6Enabled, Convert.ToUInt16(node.Port), node.PublicKey);
            }
        }

        /// <summary>
        /// Checks if there exists a friend with given friendnumber.
        /// </summary>
        /// <param name="friendnumber"></param>
        /// <returns></returns>
        private bool FriendExists(int friendnumber)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.FriendExists(tox, friendnumber);
            }
        }

        /// <summary>
        /// Retrieves the number of friends in this tox instance.
        /// </summary>
        /// <returns></returns>
        private int GetFriendlistCount()
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return (int)ToxFunctions.CountFriendlist(tox);
            }
        }

        /// <summary>
        /// Retrieves an array of friendnumbers of this tox instance.
        /// </summary>
        /// <returns></returns>
        private int[] GetFriendlist()
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.GetFriendlist(tox);
            }
        }

        /// <summary>
        /// Retrieves the name of a friendnumber.
        /// </summary>
        /// <param name="friendnumber"></param>
        /// <returns></returns>
        private string GetName(int friendnumber)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxTools.RemoveNull(ToxFunctions.GetName(tox, friendnumber));
            }
        }

        /// <summary>
        /// Retrieves the nickname of this tox instance.
        /// </summary>
        /// <returns></returns>
        public string GetSelfName()
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxTools.RemoveNull(ToxFunctions.GetSelfName(tox));
            }
        }

        /// <summary>
        /// Retrieves a DateTime object of the last time friendnumber was seen online.
        /// </summary>
        /// <param name="friend"></param>
        /// <returns></returns>
        public DateTime GetLastOnline(ToxFriend friend)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxTools.EpochToDateTime((long)ToxFunctions.GetLastOnline(tox, friend.Number));
            }
        }

        /// <summary>
        /// Retrieves the string of a 32 byte long address to share with others.
        /// </summary>
        /// <returns></returns>
        public string GetAddress()
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxTools.HexBinToString(ToxFunctions.GetAddress(tox));
            }
        }

        /// <summary>
        /// Retrieves the typing status of a friend.
        /// </summary>
        /// <param name="friendnumber"></param>
        /// <returns></returns>
        private bool GetIsTyping(int friendnumber)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.GetIsTyping(tox, friendnumber);
            }
        }

        /// <summary>
        /// Retrieves the friendnumber associated to the specified public address/id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public int GetFriendNumber(string id)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.GetFriendNumber(tox, id);
            }
        }

        /// <summary>
        /// Retrieves the status message of a friend.
        /// </summary>
        /// <param name="friendnumber"></param>
        /// <returns></returns>
        private string GetStatusMessage(int friendnumber)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxTools.RemoveNull(ToxFunctions.GetStatusMessage(tox, friendnumber));
            }
        }

        /// <summary>
        /// Retrieves the status message of this tox instance.
        /// </summary>
        /// <returns></returns>
        public string GetSelfStatusMessage()
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxTools.RemoveNull(ToxFunctions.GetSelfStatusMessage(tox));
            }
        }

        /// <summary>
        /// Retrieves the amount of friends who are currently online.
        /// </summary>
        /// <returns></returns>
        public int GetOnlineFriendsCount()
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return (int)ToxFunctions.GetNumOnlineFriends(tox);
            }
        }

        /// <summary>
        /// Retrieves a friend's connection status.
        /// </summary>
        /// <param name="friendnumber"></param>
        /// <returns></returns>
        private int GetFriendConnectionStatus(int friendnumber)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.GetFriendConnectionStatus(tox, friendnumber);
            }
        }

        /// <summary>
        /// Retrieves a friend's public id/address.
        /// </summary>
        /// <param name="friendnumber"></param>
        /// <returns></returns>
        public string GetClientID(int friendnumber)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.GetClientID(tox, friendnumber);
            }
        }

        /// <summary>
        /// Retrieves a friend's current user status.
        /// </summary>
        /// <param name="friendnumber"></param>
        /// <returns></returns>
        private ToxUserStatus GetUserStatus(int friendnumber)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.GetUserStatus(tox, friendnumber);
            }
        }

        /// <summary>
        /// Retrieves the current user status of this tox instance.
        /// </summary>
        /// <returns></returns>
        public ToxUserStatus GetSelfUserStatus()
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.GetSelfUserStatus(tox);
            }
        }

        /// <summary>
        /// Sets the name of this tox instance.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool SetName(string name)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.SetName(tox, name);
            }
        }

        /// <summary>
        /// Sets the user status of this tox instance.
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool SetUserStatus(ToxUserStatus status)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.SetUserStatus(tox, status);
            }
        }

        /// <summary>
        /// Sets the status message of this tox instance.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool SetStatusMessage(string message)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.SetStatusMessage(tox, message);
            }
        }

        /// <summary>
        /// Sets the typing status of this tox instance.
        /// </summary>
        /// <param name="friend"></param>
        /// <param name="is_typing"></param>
        /// <returns></returns>
        public bool SetUserIsTyping(ToxFriend friend, bool is_typing)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.SetUserIsTyping(tox, friend.Number, is_typing);
            }
        }

        /// <summary>
        /// Send a message to a friend.
        /// </summary>
        /// <param name="friend"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public int SendMessage(ToxFriend friend, string message)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.SendMessage(tox, friend.Number, message);
            }
        }

        /// <summary>
        /// Send a message to a friend. The given id will be used as the message id.
        /// </summary>
        /// <param name="friend"></param>
        /// <param name="id"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public int SendMessageWithID(ToxFriend friend, int id, string message)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.SendMessageWithID(tox, friend.Number, id, message);
            }
        }

        /// <summary>
        /// Sends an action to a friend.
        /// </summary>
        /// <param name="friend"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public int SendAction(ToxFriend friend, string action)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.SendAction(tox, friend.Number, action);
            }
        }

        /// <summary>
        /// Send an action to a friend. The given id will be used as the message id.
        /// </summary>
        /// <param name="friend"></param>
        /// <param name="id"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public int SendActionWithID(ToxFriend friend, int id, string message)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.SendActionWithID(tox, friend.Number, id, message);
            }
        }

        /// <summary>
        /// Saves the data of this tox instance at the given file location.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool Save(string filename)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                try
                {
                    byte[] bytes = new byte[ToxFunctions.Size(tox)];
                    ToxFunctions.Save(tox, bytes);

                    FileStream stream = new FileStream(filename, FileMode.Create);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Close();

                    return true;
                }
                catch { return false; }
            }
        }

        /// <summary>
        /// Ends the tox_do loop and kills this tox instance.
        /// </summary>
        public void Kill()
        {
            lock (obj)
            {
                thread.Abort();
                thread.Join();

                if (tox == IntPtr.Zero)
                    throw null;

                ToxFunctions.Kill(tox);
            }
        }

        /// <summary>
        /// Deletes a friend.
        /// </summary>
        /// <param name="friend"></param>
        /// <returns></returns>
        public bool DeleteFriend(ToxFriend friend)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                bool success = ToxFunctions.DeleteFriend(tox, friend.Number);

                if (success)
                    if (friends.Contains(friend))
                        friends.Remove(friend);

                return success;
            }
        }

        /// <summary>
        /// Joins a group with the given public key of the group.
        /// </summary>
        /// <param name="friend"></param>
        /// <param name="group_public_key"></param>
        /// <returns></returns>
        public ToxGroup JoinGroup(ToxFriend friend, string group_public_key)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                int result = ToxFunctions.JoinGroupchat(tox, friend.Number, group_public_key);
                if (result == -1)
                {
                    throw new Exception("Could not join group");
                }
                else
                {
                    ToxGroup group = new ToxGroup(result);
                    groups.Add(group);

                    return group;
                }
            }
        }

        /// <summary>
        /// Retrieves the name of a group member.
        /// </summary>
        /// <param name="groupnumber"></param>
        /// <param name="peernumber"></param>
        /// <returns></returns>
        private string GetGroupMemberName(int groupnumber, int peernumber)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxTools.RemoveNull(ToxFunctions.GroupPeername(tox, groupnumber, peernumber));
            }
        }

        /// <summary>
        /// Retrieves the number of group members in a group chat.
        /// </summary>
        /// <param name="groupnumber"></param>
        /// <returns></returns>
        private int GetGroupMemberCount(int groupnumber)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.GroupNumberPeers(tox, groupnumber);
            }
        }

        /// <summary>
        /// Deletes a group chat.
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public bool DeleteGroupChat(ToxGroup group)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                bool success = ToxFunctions.DeleteGroupchat(tox, group.Number);

                if (success)
                    if (groups.Contains(group))
                        groups.Remove(group);

                return success;
            }
        }

        /// <summary>
        /// Invites a friend to a group chat.
        /// </summary>
        /// <param name="friend"></param>
        /// <param name="group"></param>
        /// <returns></returns>
        public bool InviteFriend(ToxFriend friend, ToxGroup group)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.InviteFriend(tox, friend.Number, group.Number);
            }
        }

        /// <summary>
        /// Sends a message to a group.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool SendGroupMessage(ToxGroup group, string message)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.GroupMessageSend(tox, group.Number, message);
            }
        }

        /// <summary>
        /// Sends an action to a group.
        /// </summary>
        /// <param name="group"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        public bool SendGroupAction(ToxGroup group, string action)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.GroupActionSend(tox, group.Number, action);
            }
        }

        /// <summary>
        /// Creates a new group and retrieves the group number.
        /// </summary>
        /// <returns></returns>
        public ToxGroup NewGroup()
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                int result = ToxFunctions.AddGroupchat(tox);
                if (result == -1)
                {
                    throw new Exception("Could not create group");
                }
                else
                {
                    ToxGroup group = new ToxGroup(result);
                    groups.Add(group);

                    return group;
                }
            }
        }

        /// <summary>
        /// Retrieves the nospam value.
        /// </summary>
        /// <returns></returns>
        public uint GetNospam()
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                return ToxFunctions.GetNospam(tox);
            }
        }

        /// <summary>
        /// Sets the nospam value.
        /// </summary>
        /// <param name="nospam"></param>
        public void SetNospam(uint nospam)
        {
            lock (obj)
            {
                if (tox == IntPtr.Zero)
                    throw null;

                ToxFunctions.SetNospam(tox, nospam);
            }
        }

        /// <summary>
        /// Retrieves the pointer of this tox instance.
        /// </summary>
        /// <returns></returns>
        public IntPtr GetPointer()
        {
            return tox;
        }

        /// <summary>
        /// Whether to send read receipts for the specified friendnumber or not.
        /// </summary>
        /// <param name="friend"></param>
        /// <param name="send_receipts"></param>
        public void SetSendsReceipts(ToxFriend friend, bool send_receipts)
        {
            ToxFunctions.SetSendsReceipts(tox, friend.Number, send_receipts);
        }

        /// <summary>
        /// Retrieve a friend by friendnumber.
        /// </summary>
        /// <param name="friendnumber"></param>
        /// <returns></returns>
        public ToxFriend GetFriendByNumber(int friendnumber)
        {
            return friends.Find(f => f.Number == friendnumber);
        }

        /// <summary>
        /// Retrieve a group by groupnumber.
        /// </summary>
        /// <param name="groupnumber"></param>
        /// <returns></returns>
        public ToxGroup GetGroupByNumber(int groupnumber)
        {
            return groups.Find(g => g.Number == groupnumber);
        }

        private void callbacks()
        {
            ToxFunctions.CallbackFriendRequest(tox, friendrequestdelegate = new ToxDelegates.CallbackFriendRequestDelegate((IntPtr t, byte[] id, byte[] message, ushort length, IntPtr userdata) =>
            {
                if (OnFriendRequest != null)
                    Invoker(OnFriendRequest, ToxTools.RemoveNull(ToxTools.HexBinToString(id)), Encoding.UTF8.GetString(message, 0, length));
            }));

            ToxFunctions.CallbackConnectionStatus(tox, connectionstatusdelegate = new ToxDelegates.CallbackConnectionStatusDelegate((IntPtr t, int friendnumber, byte status, IntPtr userdata) =>
            {
                if (OnConnectionStatusChanged != null)
                {
                    ToxFriend friend = GetFriendByNumber(friendnumber);
                    friend.IsOnline = status == 0 ? false : true;

                    Invoker(OnConnectionStatusChanged, friend, (int)status);
                }
            }));

            ToxFunctions.CallbackFriendMessage(tox, friendmessagedelegate = new ToxDelegates.CallbackFriendMessageDelegate((IntPtr t, int friendnumber, byte[] message, ushort length, IntPtr userdata) =>
            {
                if (OnFriendMessage != null)
                {
                    ToxFriend friend = GetFriendByNumber(friendnumber);
                    Invoker(OnFriendMessage, friend, ToxTools.RemoveNull(Encoding.UTF8.GetString(message, 0, length)));
                }
            }));

            ToxFunctions.CallbackFriendAction(tox, friendactiondelegate = new ToxDelegates.CallbackFriendActionDelegate((IntPtr t, int friendnumber, byte[] action, ushort length, IntPtr userdata) =>
            {
                if (OnFriendAction != null)
                {
                    ToxFriend friend = GetFriendByNumber(friendnumber);
                    Invoker(OnFriendAction, friend, ToxTools.RemoveNull(Encoding.UTF8.GetString(action, 0, length)));
                }
            }));

            ToxFunctions.CallbackNameChange(tox, namechangedelegate = new ToxDelegates.CallbackNameChangeDelegate((IntPtr t, int friendnumber, byte[] newname, ushort length, IntPtr userdata) =>
            {
                if (OnNameChange != null)
                {
                    string name = ToxTools.RemoveNull(Encoding.UTF8.GetString(newname, 0, length));
                    ToxFriend friend = GetFriendByNumber(friendnumber);
                    friend.Name = name;

                    Invoker(OnNameChange, friend, name);
                }
            }));

            ToxFunctions.CallbackStatusMessage(tox, statusmessagedelegate = new ToxDelegates.CallbackStatusMessageDelegate((IntPtr t, int friendnumber, byte[] newstatus, ushort length, IntPtr userdata) =>
            {
                if (OnStatusMessage != null)
                {
                    string status = ToxTools.RemoveNull(Encoding.UTF8.GetString(newstatus, 0, length));
                    ToxFriend friend = GetFriendByNumber(friendnumber);
                    friend.StatusMessage = status;

                    Invoker(OnStatusMessage, friend, status);
                }
            }));

            ToxFunctions.CallbackUserStatus(tox, userstatusdelegate = new ToxDelegates.CallbackUserStatusDelegate((IntPtr t, int friendnumber, ToxUserStatus status, IntPtr userdata) =>
            {
                if (OnUserStatus != null)
                {
                    ToxFriend friend = GetFriendByNumber(friendnumber);
                    friend.Status = status;

                    Invoker(OnUserStatus, friend, status);
                }
            }));

            ToxFunctions.CallbackTypingChange(tox, typingchangedelegate = new ToxDelegates.CallbackTypingChangeDelegate((IntPtr t, int friendnumber, byte typing, IntPtr userdata) =>
            {
                bool is_typing = typing == 0 ? false : true;

                if (OnTypingChange != null)
                {
                    ToxFriend friend = GetFriendByNumber(friendnumber);
                    friend.IsTyping = is_typing;

                    Invoker(OnTypingChange, friend, is_typing);
                }
            }));

            ToxFunctions.CallbackGroupAction(tox, groupactiondelegate = new ToxDelegates.CallbackGroupActionDelegate((IntPtr t, int groupnumber, int friendgroupnumber, byte[] action, ushort length, IntPtr userdata) =>
            {
                if (OnGroupAction != null)
                {
                    ToxGroup group = GetGroupByNumber(groupnumber);
                    Invoker(OnGroupAction, group, friendgroupnumber, ToxTools.RemoveNull(Encoding.UTF8.GetString(action, 0, length)));
                }
            }));

            ToxFunctions.CallbackGroupMessage(tox, groupmessagedelegate = new ToxDelegates.CallbackGroupMessageDelegate((IntPtr t, int groupnumber, int friendgroupnumber, byte[] message, ushort length, IntPtr userdata) =>
            {
                if (OnGroupMessage != null)
                {
                    ToxGroup group = GetGroupByNumber(groupnumber);
                    Invoker(OnGroupMessage, group, friendgroupnumber, ToxTools.RemoveNull(Encoding.UTF8.GetString(message, 0, length)));
                }
            }));

            ToxFunctions.CallbackGroupInvite(tox, groupinvitedelegate = new ToxDelegates.CallbackGroupInviteDelegate((IntPtr t, int friendnumber, byte[] group_public_key, IntPtr userdata) =>
            {
                if (OnGroupInvite != null)
                {
                    ToxFriend friend = GetFriendByNumber(friendnumber);
                    Invoker(OnGroupInvite, friend, ToxTools.HexBinToString(group_public_key));
                }
            }));

            ToxFunctions.CallbackGroupNamelistChange(tox, groupnamelistchangedelegate = new ToxDelegates.CallbackGroupNamelistChangeDelegate((IntPtr t, int groupnumber, int peernumber, ToxChatChange change, IntPtr userdata) =>
            {
                if (OnGroupNamelistChange != null)
                {
                    ToxGroup group = GetGroupByNumber(groupnumber);

                    switch(change)
                    {
                        case ToxChatChange.PEER_ADD:
                            {
                                ToxGroupMember member = new ToxGroupMember(peernumber);
                                group.AddMember(member);

                                break;
                            }

                        case ToxChatChange.PEER_DEL:
                            {
                                ToxGroupMember member = group.GetGroupMemberByNumber(peernumber);
                                group.RemoveMember(member);

                                break;
                            }

                        case ToxChatChange.PEER_NAME:
                            {
                                ToxGroupMember member = group.GetGroupMemberByNumber(peernumber);
                                member.Name = GetGroupMemberName(groupnumber, peernumber);

                                break;
                            }
                    }

                    Invoker(OnGroupNamelistChange, group, peernumber, change);
                }
            }));

            ToxFunctions.CallbackFileControl(tox, filecontroldelegate = new ToxDelegates.CallbackFileControlDelegate((IntPtr t, int friendnumber, byte receive_send, byte filenumber, byte control_type, byte[] data, ushort length, IntPtr userdata) =>
            {
                if (OnFileControl != null)
                {
                    ToxFriend friend = GetFriendByNumber(friendnumber);
                    Invoker(OnFileControl, friend, receive_send, filenumber, control_type, data);
                }
            }));

            ToxFunctions.CallbackFileData(tox, filedatadelegate = new ToxDelegates.CallbackFileDataDelegate((IntPtr t, int friendnumber, byte filenumber, byte[] data, ushort length, IntPtr userdata) =>
            {
                if (OnFileData != null)
                {
                    ToxFriend friend = GetFriendByNumber(friendnumber);
                    Invoker(OnFileData, friend, filenumber, data);
                }
            }));

            ToxFunctions.CallbackFileSendRequest(tox, filesendrequestdelegate = new ToxDelegates.CallbackFileSendRequestDelegate((IntPtr t, int friendnumber, byte filenumber, ulong filesize, byte[] filename, ushort filename_length, IntPtr userdata) =>
            {
                if (OnFileSendRequest != null)
                {
                    ToxFriend friend = GetFriendByNumber(friendnumber);
                    Invoker(OnFileSendRequest, friend, filenumber, filesize, ToxTools.RemoveNull(Encoding.UTF8.GetString(filename, 0, filename_length)));
                }
            }));

            ToxFunctions.CallbackReadReceipt(tox, readreceiptdelegate = new ToxDelegates.CallbackReadReceiptDelegate((IntPtr t, int friendnumber, uint receipt, IntPtr userdata) =>
            {
                if (OnReadReceipt != null)
                {
                    ToxFriend friend = GetFriendByNumber(friendnumber);
                    Invoker(OnReadReceipt, friend, receipt);
                }
            }));
        }
    }
}

#pragma warning restore 1591