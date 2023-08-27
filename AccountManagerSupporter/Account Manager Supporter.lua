spawn(function() 
    pcall(function() loadstring(readfile("Auto Execute.txt"))() end)
end)
local GuiService = game:GetService("GuiService")
GuiService.ErrorMessageChanged:Connect(function()
    local Code = GuiService:GetErrorCode().Value
    if Code >= Enum.ConnectionError.DisconnectErrors.Value then
        getgenv().StopUpdate = true
    end
end)
repeat wait() until game:IsLoaded()
while wait(1) do 
    if not getgenv().StopUpdate and game.Players.LocalPlayer.Parent and game.Players.LocalPlayer:FindFirstChild("PlayerScripts") then 
        local s,e = pcall(function() 
            writefile("Account Manager Supporter.txt",readfile("CurrentTime.txt"))
        end)
        if e then print(e) end
    end
end