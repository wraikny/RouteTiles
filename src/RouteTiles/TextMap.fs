module internal RouteTiles.App.TextMap

type Buttons = {
  play: string
  ranking: string
  setting: string
  timeattack5000: string
  scoreattack180: string
  endless: string
  tutorial: string
  namesetting: string
  volumeSetting: string
  backgroundsetting: string
  save: string
  cancel: string
  start: string
  back: string
  continueGame: string
  restartGame: string
  quitGame: string
  changeController: string
  others: string
  keyboard: string
  sendToServer: string
  notSendToServer: string
  bgm: string
  se: string
  backgroundWaveBlue: string
  backgroundFloatingTiles: string
}

type Descriptions = {
  play: string
  ranking: string
  setting: string
  timeattack5000: string
  scoreattack180: string
  endless: string
  tutorial: string
  namesetting: string
  volumeSetting: string
  backgroundsetting: string
  settingsave: string
  selectController: string
  selectBackground: string
  continueGame: string
  restartGame: string
  quitGame: string
  changeUsername: string
  changeVolume: string
  namePlaceholder: string
  waitingResponse: string
  sendToServer: string
  error: string
}

type Modes = {
  gameModeSelect: string
  controllerSelect: string
  ranking: string
  setting: string
  nameSetting: string
  backgroundSetting: string
  pause: string
  gameResult: string
  rankingOf: string
  volumeSetting: string
  howToControl: string
  // waitingResponse: string
}

type GameInfo = {
  score: string
  time: string
  tileCount: string
  routeCount: string
  loopCount: string
}

type TextMap = {
  buttons: Buttons
  descriptions: Descriptions
  modes: Modes
  gameInfo: GameInfo
  others: string
}

let textMapJapanese = {
  buttons = {
    play = "遊ぶ"
    ranking = "ランキング"
    setting = "設定"
    timeattack5000 = "タイム5000"
    scoreattack180 = "スコア180"
    endless = "エンドレス"
    tutorial = "チュートリアル"
    namesetting = "お名前"
    volumeSetting = "音量"
    backgroundsetting = "背景"
    save = "保存する"
    cancel = "キャンセル"
    start = "スタート"
    back = "戻る"
    continueGame = "再開"
    restartGame = "はじめから"
    quitGame = "ゲームをやめる"
    changeController = "コントローラ切替"
    others = "キーボード"
    keyboard = "キーボード"
    sendToServer = "送信する"
    notSendToServer = "送信しない"
    bgm = "BGM"
    se = "SE"
    backgroundWaveBlue = "Wave"
    backgroundFloatingTiles = "FloatingTiles"
  }

  descriptions = {
    play = "ゲームを遊びます"
    ranking = "オンラインランキングを確認します"
    setting = "色々な設定を行います"
    timeattack5000 = "5000点までの時間を競うモードです"
    scoreattack180 = "180秒以内のスコアを競うモードです"
    endless = "ひたすらタイルをつなぐ練習用のモードです"
    tutorial = "ゲームの遊び方を説明します"
    namesetting = "ランキングで使う名前を設定します"
    volumeSetting = "BGMとSEの音量を設定します"
    backgroundsetting = "ゲーム中の背景を設定します"
    settingsave = "設定を保存します"
    selectController = "使いたいコントローラを選びます"
    selectBackground = "ゲーム中の背景を設定します"
    continueGame = "ゲームを再開します"
    restartGame = "ゲームを始めからやりなおします"
    quitGame = "ゲームをやめてタイトルに戻ります"
    changeUsername = "ユーザ名を設定します"
    changeVolume = "音量を設定します"
    namePlaceholder = "username"
    waitingResponse = "しばらくお待ち下さい..."
    sendToServer = "オンラインランキングに結果を送信しますか？"
    error = "エラーが発生しました"
  }

  modes = {
    gameModeSelect = "ゲームモード選択"
    controllerSelect = "コントローラー選択"
    ranking = "ランキング"
    setting = "設定"
    nameSetting = "お名前設定"
    backgroundSetting = "背景設定"
    pause = "一時停止"
    gameResult = "リザルト"
    rankingOf = "ランキング - "
    volumeSetting = "音量設定"
    howToControl = "操作説明"
    // waitingResponse = "通信待機中..."
    // error = "エラー"
  }

  gameInfo = {
    score = "スコア"
    time = "タイム"
    tileCount = "タイル数"
    routeCount = "ルート数"
    loopCount = "ループ数"
  }

  others = ":/-."
}
