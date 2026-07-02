using System.Collections.Generic;

namespace Analytics
{
    /// <summary>
    /// Predefined analytics events for the block sort game
    /// </summary>
    public static class GameAnalyticsEvents
    {
        public const string GAME_INITIALIZATION_COMPLETE = "game_initialization_complete";
        // Level Events
        public const string LEVEL_STARTED = "level_started";
        public const string LEVEL_COMPLETED = "level_completed";
        public const string LEVEL_FAILED = "level_failed";
        public const string LEVEL_RESTARTED = "level_restarted";

        // Gameplay Events
        public const string BLOCK_REMOVED = "block_removed";
        public const string GATE_PASSED = "gate_passed";

        // UI Events
        public const string UI_BUTTON_CLICKED = "ui_button_clicked";
        public const string POPUP_OPENED = "popup_opened";
        public const string POPUP_CLOSED = "popup_closed";

        // Economy Events
        public const string CURRENCY_EARNED = "currency_earned";
        public const string CURRENCY_SPENT = "currency_spent";
        public const string REWARD_RECEIVED = "reward_received";

        // Session Events
        public const string SESSION_STARTED = "session_started";
        public const string SESSION_ENDED = "session_ended";
        public const string GAME_INITIALIZED = "game_initialized";

        // Ad Events
        public const string AD_REQUESTED = "ad_requested";
        public const string AD_SHOWN = "ad_shown";
        public const string AD_CLICKED = "ad_clicked";
        public const string AD_COMPLETED = "ad_completed";
        public const string AD_FAILED = "ad_failed";

        // Lives/Ad Events  
        public const string PLAYER_START_GAME_NO_LIVES = "player_start_game_no_lives";
        public const string PLAYER_WATCH_AD_FOR_LIVES = "player_watch_ad_for_lives";
        public const string PLAYER_BUY_CURRENCY = "player_buy_currency";

        // Shop/IAP Events
        public const string SHOP_PURCHASE = "shop_purchase";
        public const string SHOP_PURCHASE_ATTEMPT = "shop_purchase_attempt";
        public const string SHOP_PURCHASE_FAILED = "shop_purchase_failed";

        // Rate App Events
        public const string RATE_APP_TRIGGER = "rate_app_trigger";
        public const string IN_APP_REVIEW_SHOWN = "in_app_review_shown";
        public const string IN_APP_REVIEW_COMPLETED = "in_app_review_completed";

        // Reward Video Events
        public const string REWARD_VIDEO_SHOWN = "reward_video_shown";

        // Additional Popup Events  
        public const string POPUP_OPEN = "popup_open";
        
        // Replay Button Event
        public const string REPLAY_BUTTON_CLICKED = "replay_button_clicked";

        // Parameter Keys
        public const string PARAM_LEVEL_INDEX = "level_index";
        public const string PARAM_LEVEL_NAME = "level_name";
        public const string PARAM_SCORE = "score";
        public const string PARAM_TIME_SPENT = "time_spent";
        public const string PARAM_ATTEMPTS = "attempts";
        public const string PARAM_FAIL_REASON = "fail_reason";
        public const string PARAM_CURRENCY_TYPE = "currency_type";
        public const string PARAM_CURRENCY_AMOUNT = "currency_amount";
        public const string PARAM_BUTTON_NAME = "button_name";
        public const string PARAM_POPUP_NAME = "popup_name";
        public const string PARAM_REWARD_TYPE = "reward_type";
        public const string PARAM_REWARD_AMOUNT = "reward_amount";
        public const string PARAM_BLOCK_COUNT = "block_count";
        public const string PARAM_AD_TYPE = "ad_type";
        public const string PARAM_AD_PLACEMENT = "ad_placement";
        public const string PARAM_ERROR_MESSAGE = "error_message";
        public const string PARAM_SESSION_LENGTH = "session_length";
        public const string PARAM_GAME_MODE = "game_mode";
        public const string PARAM_DIFFICULTY = "difficulty";
        public const string PARAM_BUILD_NUMBER = "build_number";
        public const string PARAM_PLATFORM = "platform";
        public const string PARAM_CLOSE_WIN = "close_win";
        public const string PARAM_POPUP_TYPE = "popup_type";
        public const string PARAM_ACTION = "action";
        public const string PARAM_REVIEW_TRIGGER_LEVEL = "review_trigger_level";
        public const string PARAM_REVIEW_STATUS = "review_status";
        public const string PARAM_ADS_NAME = "ads_name";
        public const string PARAM_RETENTION_DAY = "retention_day"; // Default parameter - included in all events
        public const string PARAM_COINS = "coins"; // Default parameter - player's current coins
        public const string PARAM_LIVES = "lives"; // Default parameter - player's current lives
        public const string PARAM_PRODUCT_ID = "product_id";
        public const string PARAM_PACKAGE_NAME = "package_name";
        public const string PARAM_PURCHASE_TYPE = "purchase_type";
        public const string PARAM_PRICE = "price";
        public const string PARAM_COIN_COST = "coin_cost";
        public const string PARAM_FAIL_STATUS = "fail_status";
    }
}
