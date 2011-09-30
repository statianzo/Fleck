require 'erb'

class NUnitRunner
	include FileTest

	def initialize(paths)
		@sourceDir = paths.fetch(:source, 'source')
		@resultsDir = paths.fetch(:results, 'results')
		@compilePlatform = paths.fetch(:platform, '')
		@compileTarget = paths.fetch(:compilemode, 'debug')
	
		@nunitExe = File.join('src', 'packages', 'NUnit.2.5.10.11092', 'tools', "nunit-console#{(@compilePlatform.empty? ? '' : "-#{@compilePlatform}")}.exe").gsub('/','\\') + ' /nothread'
	end
	
	def executeTests(assemblies)
		Dir.mkdir @resultsDir unless exists?(@resultsDir)
		
		assemblies.each do |assem|
			file = File.expand_path("#{@sourceDir}/#{assem}/bin/#{@compileTarget}/#{assem}.dll")
			sh "#{@nunitExe} \"#{file}\" /xml=#{@resultsDir}/#{assem}-TestResults.xml"
		end
	end
end

class MSBuildRunner
	def self.compile(attributes)
        compileTarget = attributes.fetch(:compilemode, 'debug')
        solutionFile = attributes[:solutionfile]
        
        attributes[:projFile] = solutionFile
        attributes[:properties] ||= []
        attributes[:properties] << "Configuration=#{compileTarget}"
        attributes[:extraSwitches] = ["v:m", "t:rebuild"]
        attributes[:extraSwitches] << "maxcpucount:2" unless Platform.is_nix
        
        self.runProjFile(attributes);
    end

    def self.runProjFile(attributes)
        version = attributes.fetch(:clrversion, 'v4.0.30319')
        compileTarget = attributes.fetch(:compilemode, 'debug')
        projFile = attributes[:projFile]

        if Platform.is_nix
            msbuildFile = `which xbuild`.chop
        else
            frameworkDir = File.join(ENV['windir'].dup, 'Microsoft.NET', 'Framework', version)
            msbuildFile = File.join(frameworkDir, 'msbuild.exe')
        end

        properties = attributes.fetch(:properties, [])

        switchesValue = ""

        properties.each do |prop|
            switchesValue += " /property:#{prop}"
        end	

        extraSwitches = attributes.fetch(:extraSwitches, [])	

        extraSwitches.each do |switch|
            switchesValue += " /#{switch}"
        end	

        targets = attributes.fetch(:targets, [])
        targetsValue = ""
        targets.each do |target|
            targetsValue += " /t:#{target}"
        end

        sh "#{msbuildFile} #{projFile} #{targetsValue} #{switchesValue}"
    end
end